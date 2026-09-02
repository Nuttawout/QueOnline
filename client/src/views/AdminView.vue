<script setup>
import { nextTick, reactive, ref, watch } from 'vue';
import { adminLogin, updateAdminSettings, uploadAdminLogo, uploadAdminSound } from '../api/admin';
import { apiBaseUrl } from '../api/http';
import { branchStorageKey } from '../composables/useBranch';
import { settings, applySettings } from '../composables/useSettings';
import { SOUND_PRESETS, playChime } from '../composables/useQueueAlert';
import { FONT_OPTIONS, FONT_SIZE_OPTIONS } from '../config/fonts';
import {
  FIELD_DEFS,
  FIELD_GROUPS,
  DEFAULT_FIELD_LAYOUT,
  normalizeFieldLayout,
  ALIGN_OPTIONS,
  VALIGN_OPTIONS,
} from '../config/fieldLayout';
import QueueStatusCard from '../components/QueueStatusCard.vue';
import { CARD_TEMPLATES, DEFAULT_CARD_TEMPLATE } from '../config/cardTemplates';
import { TEMPLATE_COMPONENTS } from '../components/templates/registry';

// Fixed sample data for the admin's live preview — the admin page never has real queue data to
// hand, and the preview must always show all 5 timeline stages regardless of which field the
// admin happens to be dragging right now.
const PREVIEW_RESULT = {
  qNumber: 'A5',
  patientName: 'นาย ตัวอย่าง ใจดี',
  hn: '123456',
  currentServingNumber: 'A3',
  aheadCount: 2,
  estimatedWaitMinutes: 12,
  estimatedWaitSampleSize: 4,
  departmentName: 'อายุรกรรม',
  groupName: null,
  visitNumber: '1',
  // "lab" is flagged skipped so every template's live preview demonstrates all four step states
  // at once — done (register), current (call), skipped (lab — e.g. no lab test ordered), pending
  // (fin/pha) — instead of admins only ever seeing skipped by editing real data.
  timeline: [
    { key: 'register', label: 'ลงทะเบียน', location: null, at: new Date().toISOString(), done: true },
    { key: 'call', label: 'เรียกเข้าห้องตรวจ', location: 'ห้องตรวจ1', at: null, done: false },
    { key: 'lab', label: 'ตรวจแล็ป', location: '', at: null, done: false, skipped: true },
    { key: 'fin', label: 'การเงิน', location: '', at: null, done: false },
    { key: 'pha', label: 'ห้องยา', location: '', at: null, done: false },
  ],
};
const previewArrived = ref(false);

// Branch-scoped storage key — without this, a token from a previously visited branch (still
// sitting in this same-origin sessionStorage) would look "logged in" on a different branch's
// admin page even though the server rejects it as soon as anything is actually saved.
const TOKEN_KEY = branchStorageKey('qo_admin_token');

const token = ref(sessionStorage.getItem(TOKEN_KEY) || '');
const password = ref('');
const loginError = ref('');
const loginLoading = ref(false);

const form = reactive({
  hospitalName: '',
  tagline: '',
  greeting: '',
  arrivedTitle: '',
  arrivedBody: '',
  primaryColor: '#102a4d',
  accentColor: '#e8720f',
  fontFamily: 'Anuphan',
  fontSizeScale: 'medium',
  soundChoice: 'soft',
  cardTemplate: DEFAULT_CARD_TEMPLATE,
});

const fieldLayout = reactive({ ...DEFAULT_FIELD_LAYOUT });
const selectedFieldId = ref(null);

function templateComponentFor(id) {
  return TEMPLATE_COMPONENTS[id] ?? null;
}

function fieldsInGroup(groupId) {
  return FIELD_DEFS.filter((f) => f.group === groupId);
}

const saving = ref(false);
const saveMessage = ref('');
const saveError = ref('');

const logoFile = ref(null);
const logoUploading = ref(false);
const logoError = ref('');

const soundFile = ref(null);
const soundUploading = ref(false);
const soundError = ref('');

// Suppress the auto-preview watcher while we're programmatically filling the form from
// settings — it should only fire when the admin actually changes the dropdown.
let allowAutoPreview = false;

function initFormFromSettings() {
  allowAutoPreview = false;
  form.hospitalName = settings.hospitalName;
  form.tagline = settings.tagline;
  form.greeting = settings.greeting;
  form.arrivedTitle = settings.arrivedTitle;
  form.arrivedBody = settings.arrivedBody;
  form.primaryColor = settings.primaryColor;
  form.accentColor = settings.accentColor;
  form.fontFamily = settings.fontFamily;
  form.fontSizeScale = settings.fontSizeScale;
  form.soundChoice = settings.soundChoice;
  form.cardTemplate = settings.cardTemplate ?? DEFAULT_CARD_TEMPLATE;
  Object.assign(fieldLayout, normalizeFieldLayout(settings.fieldLayout));
  nextTick(() => {
    allowAutoPreview = true;
  });
}

function onDragField(id, x, y) {
  fieldLayout[id].x = x;
  fieldLayout[id].y = y;
  selectedFieldId.value = id;
}

function resetField(id) {
  Object.assign(fieldLayout[id], DEFAULT_FIELD_LAYOUT[id]);
}

function resetAllFields() {
  Object.assign(fieldLayout, normalizeFieldLayout(null));
}

// "Auto" — recomputes clean, evenly-spaced Y positions for whatever fields are currently visible
// (closing the gaps left by hidden fields, which dragging alone can't fix) while restoring each
// field's default X so fields that are meant to line up (the two stat boxes, every timeline step)
// actually line up again after being dragged around. Fields sharing the same default Y (e.g. the
// two stat boxes) are treated as one row and kept on the same new Y together.
function autoArrangeFields() {
  const visible = FIELD_DEFS.filter((def) => fieldLayout[def.id].visible);
  if (visible.length === 0) return;

  const rowsByDefaultY = new Map();
  for (const def of visible) {
    const defaultY = DEFAULT_FIELD_LAYOUT[def.id].y;
    if (!rowsByDefaultY.has(defaultY)) rowsByDefaultY.set(defaultY, []);
    rowsByDefaultY.get(defaultY).push(def.id);
  }
  const rowYs = [...rowsByDefaultY.keys()].sort((a, b) => a - b);

  const top = 8;
  const bottom = 96;
  const step = rowYs.length > 1 ? (bottom - top) / (rowYs.length - 1) : 0;

  rowYs.forEach((defaultY, i) => {
    const newY = Math.round((rowYs.length > 1 ? top + step * i : (top + bottom) / 2) * 10) / 10;
    for (const id of rowsByDefaultY.get(defaultY)) {
      fieldLayout[id].y = newY;
      fieldLayout[id].x = DEFAULT_FIELD_LAYOUT[id].x;
    }
  });
}

watch(
  () => settings.loaded,
  (loaded) => {
    if (loaded) initFormFromSettings();
  },
  { immediate: true },
);

watch(
  () => form.soundChoice,
  () => {
    if (allowAutoPreview) testSound();
  },
);

const soundItems = () => {
  const items = Object.entries(SOUND_PRESETS).map(([value, p]) => ({ value, title: p.label }));
  if (settings.customSoundUrl) {
    items.splice(items.length - 1, 0, { value: 'custom', title: 'เสียงที่อัปโหลดเอง' });
  }
  return items;
};

async function login() {
  if (!password.value) return;
  loginLoading.value = true;
  loginError.value = '';
  try {
    const data = await adminLogin(password.value);
    token.value = data.token;
    sessionStorage.setItem(TOKEN_KEY, data.token);
    password.value = '';
  } catch (err) {
    loginError.value = err?.response?.status === 401 ? 'รหัสผ่านไม่ถูกต้อง' : 'เข้าสู่ระบบไม่สำเร็จ กรุณาลองใหม่';
  } finally {
    loginLoading.value = false;
  }
}

function logout() {
  token.value = '';
  sessionStorage.removeItem(TOKEN_KEY);
}

async function save() {
  saving.value = true;
  saveMessage.value = '';
  saveError.value = '';
  try {
    const updated = await updateAdminSettings(token.value, { ...form, fieldLayoutJson: JSON.stringify(fieldLayout) });
    applySettings(updated);
    saveMessage.value = 'บันทึกเรียบร้อยแล้ว';
  } catch (err) {
    if (err?.response?.status === 401) {
      saveError.value = 'เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่';
      logout();
    } else {
      saveError.value = 'บันทึกไม่สำเร็จ กรุณาลองใหม่';
    }
  } finally {
    saving.value = false;
  }
}

// VFileInput models as File[] even in single-file mode.
function firstFile(model) {
  return Array.isArray(model) ? model[0] : model;
}

async function uploadLogo() {
  const file = firstFile(logoFile.value);
  if (!file) return;
  logoUploading.value = true;
  logoError.value = '';
  try {
    const { logoUrl } = await uploadAdminLogo(token.value, file);
    applySettings({ logoUrl });
    logoFile.value = null;
  } catch (err) {
    if (err?.response?.status === 401) {
      logoError.value = 'เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่';
      logout();
    } else {
      logoError.value = err?.response?.data?.message ?? 'อัปโหลดโลโก้ไม่สำเร็จ';
    }
  } finally {
    logoUploading.value = false;
  }
}

async function uploadSound() {
  const file = firstFile(soundFile.value);
  if (!file) return;
  soundUploading.value = true;
  soundError.value = '';
  try {
    const updated = await uploadAdminSound(token.value, file);
    applySettings(updated);
    form.soundChoice = updated.soundChoice; // server switches active choice to "custom" on upload
    soundFile.value = null;
  } catch (err) {
    if (err?.response?.status === 401) {
      soundError.value = 'เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่';
      logout();
    } else {
      soundError.value = err?.response?.data?.message ?? 'อัปโหลดเสียงไม่สำเร็จ';
    }
  } finally {
    soundUploading.value = false;
  }
}

function testSound() {
  playChime(false, form.soundChoice);
}
</script>

<template>
  <v-container style="max-width: 560px" class="py-8">
    <h1 class="text-h5 font-weight-medium mb-6">ตั้งค่าระบบ (Admin)</h1>

    <!-- Login -->
    <v-card v-if="!token" class="pa-6" elevation="4">
      <div class="text-subtitle-1 font-weight-medium mb-3">เข้าสู่ระบบ</div>
      <v-form @submit.prevent="login">
        <v-text-field
          v-model="password"
          label="รหัสผ่านแอดมิน"
          type="password"
          variant="outlined"
          autofocus
          class="mb-3"
        />
        <v-btn type="submit" color="primary" block :loading="loginLoading">เข้าสู่ระบบ</v-btn>
      </v-form>
      <v-alert v-if="loginError" type="error" variant="tonal" density="compact" class="mt-4">
        {{ loginError }}
      </v-alert>
    </v-card>

    <!-- Settings editor -->
    <template v-else>
      <v-card class="pa-6 mb-4" elevation="4">
        <div class="text-subtitle-1 font-weight-medium mb-4">ข้อมูลโรงพยาบาล</div>
        <v-text-field v-model="form.hospitalName" label="ชื่อโรงพยาบาล" variant="outlined" class="mb-2" />
        <v-text-field v-model="form.tagline" label="Tagline (ข้อความใต้ชื่อ)" variant="outlined" class="mb-2" />
        <v-text-field v-model="form.greeting" label="คำทักทาย (หน้าแรก)" variant="outlined" class="mb-2" />

        <v-divider class="my-4" />

        <div class="text-subtitle-2 mb-2">ข้อความตอน "ถึงคิวแล้ว"</div>
        <v-text-field v-model="form.arrivedTitle" label="หัวข้อ" variant="outlined" class="mb-2" />
        <v-text-field v-model="form.arrivedBody" label="ข้อความรอง" variant="outlined" class="mb-2" />

        <v-divider class="my-4" />

        <div class="text-subtitle-2 mb-2">ฟอนต์</div>
        <v-select
          v-model="form.fontFamily"
          :items="FONT_OPTIONS.map((f) => ({ value: f.value, title: f.label }))"
          variant="outlined"
          density="compact"
          class="mb-3"
          :style="{ fontFamily: form.fontFamily }"
        />

        <div class="text-subtitle-2 mb-2">ขนาดตัวอักษร</div>
        <v-select
          v-model="form.fontSizeScale"
          :items="FONT_SIZE_OPTIONS.map((s) => ({ value: s.value, title: s.label }))"
          variant="outlined"
          density="compact"
          class="mb-3"
        />

        <div class="text-subtitle-2 mb-2">สีธีมหลัก (โครงสร้าง เช่น หัวข้อ ปุ่ม แถบด้านล่าง)</div>
        <div class="d-flex align-center ga-3 mb-3">
          <input v-model="form.primaryColor" type="color" class="color-input" />
          <v-text-field v-model="form.primaryColor" variant="outlined" density="compact" hide-details class="flex-grow-1" />
        </div>

        <div class="text-subtitle-2 mb-2">สีเน้น (ตัวเลขคิวใหญ่ และเวลารอโดยประมาณ)</div>
        <div class="d-flex align-center ga-3 mb-2">
          <input v-model="form.accentColor" type="color" class="color-input" />
          <v-text-field v-model="form.accentColor" variant="outlined" density="compact" hide-details class="flex-grow-1" />
        </div>

        <v-divider class="my-4" />

        <div class="text-subtitle-2 mb-2">เสียงแจ้งเตือน (เปลี่ยนตัวเลือกจะเล่นตัวอย่างให้ฟังอัตโนมัติ)</div>
        <div class="d-flex align-center ga-2">
          <v-select v-model="form.soundChoice" :items="soundItems()" variant="outlined" density="compact" hide-details class="flex-grow-1" />
          <v-btn variant="outlined" icon="mdi-volume-high" @click="testSound" />
        </div>

        <v-btn color="primary" size="large" block class="mt-6" :loading="saving" @click="save">
          บันทึกการตั้งค่า
        </v-btn>
        <v-alert v-if="saveMessage" type="success" variant="tonal" density="compact" class="mt-3">
          {{ saveMessage }}
        </v-alert>
        <v-alert v-if="saveError" type="error" variant="tonal" density="compact" class="mt-3">
          {{ saveError }}
        </v-alert>
      </v-card>

      <v-card class="pa-6 mb-4" elevation="4">
        <div class="text-subtitle-1 font-weight-medium mb-1">รูปแบบการ์ดคิว</div>
        <div class="text-caption text-medium-emphasis mb-4">
          เลือกรูปแบบสำเร็จรูป 5 แบบ (โครงสร้างต่างกันจริง ไม่ใช่แค่เปลี่ยนสี) หรือ "กำหนดเอง" เพื่อลาก-วางเองแบบอิสระ
        </div>

        <div class="template-grid mb-3">
          <button
            v-for="tpl in CARD_TEMPLATES"
            :key="tpl.id"
            type="button"
            class="template-option"
            :class="{ active: form.cardTemplate === tpl.id }"
            @click="form.cardTemplate = tpl.id"
          >
            <div class="template-thumb">
              <div class="template-thumb-inner">
                <component
                  :is="templateComponentFor(tpl.id)"
                  :result="PREVIEW_RESULT"
                  :arrived="false"
                  current-step-key="call"
                  :displayed-ahead="PREVIEW_RESULT.aheadCount"
                  formatted-wait="12 นาที"
                  :arrived-title="form.arrivedTitle"
                  :arrived-body="form.arrivedBody"
                />
              </div>
            </div>
            <div class="template-name">{{ tpl.label }}</div>
          </button>

          <button
            type="button"
            class="template-option"
            :class="{ active: form.cardTemplate === 'custom' }"
            @click="form.cardTemplate = 'custom'"
          >
            <div class="template-thumb">
              <div class="template-thumb-inner">
                <QueueStatusCard
                  :result="PREVIEW_RESULT"
                  :arrived="false"
                  current-step-key="call"
                  :displayed-ahead="PREVIEW_RESULT.aheadCount"
                  formatted-wait="12 นาที"
                  :arrived-title="form.arrivedTitle"
                  :arrived-body="form.arrivedBody"
                  :field-layout="fieldLayout"
                />
              </div>
            </div>
            <div class="template-name">กำหนดเอง</div>
          </button>
        </div>

        <div class="text-caption text-medium-emphasis mb-4">
          {{
            form.cardTemplate === 'custom'
              ? 'ลาก-วางเองอิสระทุกตำแหน่ง/สี/ขนาด — แก้ไขในการ์ด "จัดวางหน้าจอ" ด้านล่าง'
              : CARD_TEMPLATES.find((t) => t.id === form.cardTemplate)?.description
          }}
        </div>

        <template v-if="form.cardTemplate !== 'custom'">
          <div class="d-flex justify-center mb-3">
            <v-btn-toggle v-model="previewArrived" mandatory density="compact" color="primary" variant="outlined">
              <v-btn :value="false" size="small">กำลังรอคิว</v-btn>
              <v-btn :value="true" size="small">ถึงคิวแล้ว</v-btn>
            </v-btn-toggle>
          </div>
          <div class="mobile-frame mx-auto">
            <component
              :is="templateComponentFor(form.cardTemplate)"
              :result="PREVIEW_RESULT"
              :arrived="previewArrived"
              current-step-key="call"
              :displayed-ahead="PREVIEW_RESULT.aheadCount"
              formatted-wait="12 นาที"
              :arrived-title="form.arrivedTitle"
              :arrived-body="form.arrivedBody"
            />
          </div>
        </template>

        <v-btn color="primary" size="large" block class="mt-4" :loading="saving" @click="save">
          บันทึกการตั้งค่า
        </v-btn>
        <v-alert v-if="saveMessage" type="success" variant="tonal" density="compact" class="mt-3">
          {{ saveMessage }}
        </v-alert>
        <v-alert v-if="saveError" type="error" variant="tonal" density="compact" class="mt-3">
          {{ saveError }}
        </v-alert>
      </v-card>

      <v-card v-if="form.cardTemplate === 'custom'" class="pa-6 mb-4" elevation="4">
        <div class="text-subtitle-1 font-weight-medium mb-1">จัดวางหน้าจอ (ขนาด/สี/ตำแหน่ง/ซ่อน-โชว์)</div>
        <div class="text-caption text-medium-emphasis mb-4">
          ลากแต่ละช่องในพรีวิวเพื่อย้ายตำแหน่ง — พรีวิวนี้คือหน้าตาจริงที่ผู้ป่วยจะเห็น (ใช้ข้อมูลตัวอย่าง)
        </div>

        <div class="d-flex justify-center mb-3">
          <v-btn-toggle v-model="previewArrived" mandatory density="compact" color="primary" variant="outlined">
            <v-btn :value="false" size="small">กำลังรอคิว</v-btn>
            <v-btn :value="true" size="small">ถึงคิวแล้ว</v-btn>
          </v-btn-toggle>
        </div>

        <div class="mobile-frame mx-auto mb-4">
          <QueueStatusCard
            :result="PREVIEW_RESULT"
            :arrived="previewArrived"
            current-step-key="call"
            :displayed-ahead="PREVIEW_RESULT.aheadCount"
            formatted-wait="12 นาที"
            :arrived-title="form.arrivedTitle"
            :arrived-body="form.arrivedBody"
            :field-layout="fieldLayout"
            editable
            @drag-field="onDragField"
          />
        </div>

        <div v-for="group in FIELD_GROUPS" :key="group.id" class="field-group">
          <div class="field-group-header">
            <span>{{ group.label }}</span>
            <span class="field-group-count">
              {{ fieldsInGroup(group.id).filter((f) => fieldLayout[f.id].visible).length }}/{{ fieldsInGroup(group.id).length }} แสดงอยู่
            </span>
          </div>

          <div v-for="def in fieldsInGroup(group.id)" :key="def.id" class="field-row">
            <div class="field-row-main" @click="selectedFieldId = selectedFieldId === def.id ? null : def.id">
              <v-switch
                v-model="fieldLayout[def.id].visible"
                density="compact"
                color="primary"
                hide-details
                class="flex-grow-0"
                @click.stop
              />
              <span class="field-label">{{ def.label }}</span>
              <v-icon
                :icon="selectedFieldId === def.id ? 'mdi-chevron-up' : 'mdi-chevron-down'"
                size="18"
                class="text-medium-emphasis"
              />
            </div>
            <div v-if="selectedFieldId === def.id" class="field-row-controls">
              <div class="d-flex align-center flex-wrap ga-2 w-100">
                <span class="text-caption text-medium-emphasis position-label">ตำแหน่ง</span>
                <span class="text-caption text-medium-emphasis">X</span>
                <v-number-input
                  v-model="fieldLayout[def.id].x"
                  density="compact"
                  variant="outlined"
                  hide-details
                  control-variant="stacked"
                  :min="0"
                  :max="100"
                  suffix="%"
                  class="xy-input"
                />
                <span class="text-caption text-medium-emphasis">Y</span>
                <v-number-input
                  v-model="fieldLayout[def.id].y"
                  density="compact"
                  variant="outlined"
                  hide-details
                  control-variant="stacked"
                  :min="0"
                  :max="100"
                  suffix="%"
                  class="xy-input"
                />
              </div>

              <!-- Timeline steps keep their own fixed icon+label layout (see QueueStatusCard.vue)
                   — align/vAlign wouldn't do anything useful there, so skip the controls for them. -->
              <template v-if="def.group !== 'timeline'">
                <div class="d-flex align-center flex-wrap ga-2 w-100 mt-2">
                  <span class="text-caption text-medium-emphasis position-label">แนวนอน</span>
                  <v-btn-toggle v-model="fieldLayout[def.id].align" mandatory density="compact" color="primary" variant="outlined">
                    <v-btn v-for="opt in ALIGN_OPTIONS" :key="opt.value" :value="opt.value" size="small" :icon="opt.icon" />
                  </v-btn-toggle>
                  <span class="text-caption text-medium-emphasis ml-2">แนวตั้ง</span>
                  <v-btn-toggle v-model="fieldLayout[def.id].vAlign" mandatory density="compact" color="primary" variant="outlined">
                    <v-btn v-for="opt in VALIGN_OPTIONS" :key="opt.value" :value="opt.value" size="small" :icon="opt.icon" />
                  </v-btn-toggle>
                </div>
                <div class="text-caption text-medium-emphasis mt-1 align-hint">
                  X = ระยะห่างจาก{{ { left: 'ขอบซ้าย', right: 'ขอบขวา', center: 'กึ่งกลางแนวนอน' }[fieldLayout[def.id].align] }},
                  Y = ระยะห่างจาก{{ { top: 'ขอบบน', bottom: 'ขอบล่าง', middle: 'กึ่งกลางแนวตั้ง' }[fieldLayout[def.id].vAlign] }}
                </div>
              </template>

              <div class="d-flex align-center flex-wrap ga-2 w-100 mt-2">
                <span class="text-caption text-medium-emphasis">สี</span>
                <input v-model="fieldLayout[def.id].color" type="color" class="color-input-sm" />
                <span class="text-caption text-medium-emphasis ml-2">ขนาด</span>
                <v-number-input
                  v-model="fieldLayout[def.id].fontSize"
                  density="compact"
                  variant="outlined"
                  hide-details
                  control-variant="stacked"
                  :min="8"
                  :max="80"
                  class="font-size-input"
                />
                <v-btn
                  variant="text"
                  size="small"
                  density="compact"
                  prepend-icon="mdi-restore"
                  class="ml-auto"
                  @click="resetField(def.id)"
                >
                  รีเซ็ต
                </v-btn>
              </div>
            </div>
          </div>
        </div>

        <div class="d-flex flex-wrap ga-2 mt-2">
          <v-btn variant="outlined" size="small" prepend-icon="mdi-backup-restore" @click="resetAllFields">
            รีเซ็ตตำแหน่งทั้งหมด
          </v-btn>
          <v-btn variant="tonal" color="primary" size="small" prepend-icon="mdi-auto-fix" @click="autoArrangeFields">
            จัดเรียงอัตโนมัติ (Auto)
          </v-btn>
        </div>

        <v-btn color="primary" size="large" block class="mt-4" :loading="saving" @click="save">
          บันทึกการตั้งค่า
        </v-btn>
      </v-card>

      <v-card class="pa-6 mb-4" elevation="4">
        <div class="text-subtitle-1 font-weight-medium mb-4">โลโก้โรงพยาบาล</div>
        <div v-if="settings.logoUrl" class="mb-3">
          <img :src="apiBaseUrl + settings.logoUrl" alt="โลโก้ปัจจุบัน" class="current-logo" />
        </div>
        <v-file-input
          v-model="logoFile"
          label="เลือกไฟล์โลโก้ (.png .jpg .svg .webp)"
          accept="image/png,image/jpeg,image/svg+xml,image/webp"
          variant="outlined"
          density="compact"
          class="mb-2"
        />
        <v-btn color="primary" :disabled="!firstFile(logoFile)" :loading="logoUploading" @click="uploadLogo">
          อัปโหลดโลโก้
        </v-btn>
        <v-alert v-if="logoError" type="error" variant="tonal" density="compact" class="mt-3">
          {{ logoError }}
        </v-alert>
      </v-card>

      <v-card class="pa-6 mb-4" elevation="4">
        <div class="text-subtitle-1 font-weight-medium mb-4">อัปโหลดเสียงแจ้งเตือนเอง</div>
        <div v-if="settings.customSoundUrl" class="text-caption text-medium-emphasis mb-3">
          มีไฟล์เสียงที่อัปโหลดไว้แล้ว — เลือก "เสียงที่อัปโหลดเอง" ในช่องเสียงแจ้งเตือนด้านบนเพื่อใช้งาน
        </div>
        <v-file-input
          v-model="soundFile"
          label="เลือกไฟล์เสียง (.mp3 .wav .ogg)"
          accept="audio/mpeg,audio/wav,audio/ogg"
          variant="outlined"
          density="compact"
          class="mb-2"
        />
        <v-btn color="primary" :disabled="!firstFile(soundFile)" :loading="soundUploading" @click="uploadSound">
          อัปโหลดเสียง
        </v-btn>
        <v-alert v-if="soundError" type="error" variant="tonal" density="compact" class="mt-3">
          {{ soundError }}
        </v-alert>
      </v-card>

      <div class="text-center">
        <a href="#" class="text-caption text-medium-emphasis" @click.prevent="logout">ออกจากระบบ</a>
      </div>
    </template>
  </v-container>
</template>

<style scoped>
.color-input {
  width: 48px;
  height: 40px;
  border: 1px solid var(--n-border);
  border-radius: var(--r-control);
  padding: var(--sp-1);
  background: none;
}

.current-logo {
  max-width: 160px;
  max-height: 100px;
  object-fit: contain;
}

.mobile-frame {
  width: 260px;
  max-width: 100%;
  /* Matches the real page's `v-card class="status-card pa-3"` (Vuetify pa-3 = 12px) so a fixed
     preset template — several of which size/bleed elements assuming that padding exists (see
     TemplateHero.vue's banner) — renders here exactly as it will for a real patient. */
  padding: 12px;
  border: 8px solid var(--n-text);
  border-radius: var(--r-card-lg);
  overflow: hidden;
  background: var(--n-bg);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.18);
}

.template-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--sp-2);
}

.template-option {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: var(--sp-1);
  padding: var(--sp-1);
  border: 2px solid var(--n-border);
  border-radius: var(--r-control);
  background: var(--n-card);
  cursor: pointer;
  font: inherit;
  color: inherit;
  text-align: center;
  /* Without this, a grid item's default min-width:auto floors it at its content's min-content
     width — and .template-thumb-inner's un-shrunk 250%-width content (the `transform: scale()`
     trick below doesn't shrink layout size, only paint) counts as that content, so a template
     with a long unbroken line was inflating its whole 1fr column past its siblings'. */
  min-width: 0;
}

.template-option:hover {
  border-color: rgba(var(--v-theme-primary), 0.4);
}

.template-option.active {
  border-color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.05);
}

.template-thumb {
  aspect-ratio: 3 / 4;
  overflow: hidden;
  border-radius: 6px;
  background: var(--n-bg);
  border: 1px solid var(--n-border);
  position: relative;
}

/* Scale trick: render each template at a realistic ~350px width (250% of a ~140px thumb slot),
   then shrink the whole thing down to fit — text wraps the same way it would on a real phone
   instead of reflowing to fit a tiny box, so the thumbnail is an accurate mini version, not a
   distorted one. */
.template-thumb-inner {
  width: 250%;
  padding: 0.75rem;
  transform: scale(0.4);
  transform-origin: top left;
  pointer-events: none;
}

.template-name {
  font-size: var(--fs-xs);
  font-weight: var(--fw-medium);
}

.field-group {
  background: rgba(0, 0, 0, 0.025);
  border: 1px solid var(--n-border);
  border-radius: var(--r-control);
  padding: var(--sp-2) var(--sp-3) var(--sp-3);
  margin-bottom: var(--sp-3);
}

.field-group-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  padding: var(--sp-2) var(--sp-1) var(--sp-1);
  font-size: var(--fs-xs);
  font-weight: var(--fw-medium);
  color: rgb(var(--v-theme-primary));
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.field-group-count {
  font-size: var(--fs-xs);
  font-weight: var(--fw-regular);
  text-transform: none;
  letter-spacing: normal;
  color: var(--n-text-muted);
}

.field-row {
  border-radius: var(--r-control);
  background: var(--n-card);
  margin-bottom: var(--sp-1);
  border: 1px solid var(--n-border);
  overflow: hidden;
}

.field-row:last-child {
  margin-bottom: 0;
}

.field-row-main {
  display: flex;
  align-items: center;
  padding: var(--sp-1) var(--sp-3);
  cursor: pointer;
  min-height: 44px;
}

.field-row-main:hover {
  background: rgba(var(--v-theme-primary), 0.04);
}

.field-row-main :deep(.v-switch) {
  flex: 0 0 auto;
}

.field-row-main :deep(.v-selection-control) {
  min-height: unset;
}

.field-label {
  flex: 1 1 auto;
  font-size: var(--fs-base);
  margin-left: var(--sp-2);
}

.field-row-controls {
  display: flex;
  flex-direction: column;
  padding: var(--sp-2) var(--sp-3) var(--sp-3);
  border-top: 1px dashed var(--n-border);
  background: rgba(var(--v-theme-primary), 0.03);
}

.position-label {
  min-width: 52px;
}

.xy-input {
  width: 100px;
  flex-shrink: 0;
}

.color-input-sm {
  width: 32px;
  height: 32px;
  border: 1px solid var(--n-border);
  border-radius: var(--r-control);
  padding: var(--sp-1);
  background: none;
  flex-shrink: 0;
}

.font-size-input {
  width: 84px;
  flex-shrink: 0;
}
</style>
