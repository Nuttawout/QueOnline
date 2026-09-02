<script setup>
import { computed, reactive, ref } from 'vue';
import { superAdminLogin, fetchBranches, upsertBranch, deleteBranch } from '../api/superAdmin';

const TOKEN_KEY = 'qo_super_admin_token';
const SLUG_PATTERN = /^[a-z0-9]+(-[a-z0-9]+)*$/;

const token = ref(sessionStorage.getItem(TOKEN_KEY) || '');
const password = ref('');
const loginError = ref('');
const loginLoading = ref(false);

const branches = ref([]);
const loadingBranches = ref(false);
const loadError = ref('');

// null = form closed; 'new' = create form; a slug string = editing that existing branch.
const editingSlug = ref(null);
const form = reactive({
  slug: '',
  displayName: '',
  serverName: '',
  databaseName: '',
  dbLogin: '',
  dbPassword: '',
  encryptTransfer: true,
  adminPassword: '',
});
const formError = ref('');
const formSaving = ref(false);

const confirmDeleteSlug = ref(null);
const deleteError = ref('');

const isNew = computed(() => editingSlug.value === 'new');

async function login() {
  loginError.value = '';
  loginLoading.value = true;
  try {
    const data = await superAdminLogin(password.value);
    token.value = data.token;
    sessionStorage.setItem(TOKEN_KEY, data.token);
    password.value = '';
    await loadBranches();
  } catch (err) {
    loginError.value = err?.response?.status === 401 ? 'รหัสผ่านไม่ถูกต้อง' : 'เข้าสู่ระบบไม่สำเร็จ กรุณาลองใหม่';
  } finally {
    loginLoading.value = false;
  }
}

function logout() {
  token.value = '';
  sessionStorage.removeItem(TOKEN_KEY);
  branches.value = [];
}

async function loadBranches() {
  loadingBranches.value = true;
  loadError.value = '';
  try {
    branches.value = await fetchBranches(token.value);
  } catch (err) {
    if (err?.response?.status === 401) {
      loadError.value = 'เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่';
      logout();
    } else {
      loadError.value = 'โหลดรายชื่อสาขาไม่สำเร็จ กรุณาลองใหม่';
    }
  } finally {
    loadingBranches.value = false;
  }
}

if (token.value) {
  loadBranches();
}

function openNewForm() {
  editingSlug.value = 'new';
  formError.value = '';
  Object.assign(form, {
    slug: '',
    displayName: '',
    serverName: '',
    databaseName: '',
    dbLogin: '',
    dbPassword: '',
    encryptTransfer: true,
    adminPassword: '',
  });
}

function openEditForm(branch) {
  editingSlug.value = branch.slug;
  formError.value = '';
  Object.assign(form, {
    slug: branch.slug,
    displayName: branch.displayName,
    serverName: branch.serverName,
    databaseName: branch.databaseName,
    dbLogin: branch.dbLogin,
    dbPassword: branch.dbPassword,
    encryptTransfer: branch.encryptTransfer,
    adminPassword: branch.adminPassword,
  });
}

function closeForm() {
  editingSlug.value = null;
  formError.value = '';
}

async function saveForm() {
  formError.value = '';

  if (isNew.value && !SLUG_PATTERN.test(form.slug)) {
    formError.value = 'รหัสสาขา (slug) ต้องเป็นตัวพิมพ์เล็ก a-z, 0-9 และ - เท่านั้น เช่น vibharam-sriracha';
    return;
  }
  if (!form.displayName || !form.serverName || !form.databaseName || !form.dbLogin || !form.dbPassword || !form.adminPassword) {
    formError.value = 'กรุณากรอกให้ครบทุกช่อง';
    return;
  }

  formSaving.value = true;
  try {
    await upsertBranch(token.value, form.slug, {
      displayName: form.displayName,
      serverName: form.serverName,
      databaseName: form.databaseName,
      dbLogin: form.dbLogin,
      dbPassword: form.dbPassword,
      encryptTransfer: form.encryptTransfer,
      adminPassword: form.adminPassword,
    });
    closeForm();
    await loadBranches();
  } catch (err) {
    if (err?.response?.status === 401) {
      formError.value = 'เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่';
      logout();
    } else {
      formError.value = err?.response?.data?.message ?? 'บันทึกไม่สำเร็จ กรุณาลองใหม่';
    }
  } finally {
    formSaving.value = false;
  }
}

function askDelete(slug) {
  confirmDeleteSlug.value = slug;
  deleteError.value = '';
}

function cancelDelete() {
  confirmDeleteSlug.value = null;
}

async function confirmDelete(slug) {
  deleteError.value = '';
  try {
    await deleteBranch(token.value, slug);
    confirmDeleteSlug.value = null;
    await loadBranches();
  } catch (err) {
    if (err?.response?.status === 401) {
      deleteError.value = 'เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่';
      logout();
    } else {
      deleteError.value = 'ลบไม่สำเร็จ กรุณาลองใหม่';
    }
  }
}
</script>

<template>
  <v-container style="max-width: 720px" class="py-8">
    <h1 class="text-h5 font-weight-medium mb-1">จัดการสาขาทั้งหมด (Super Admin)</h1>
    <div class="text-body-2 text-medium-emphasis mb-6">
      เพิ่ม/แก้ไขข้อมูลเชื่อมต่อฐานข้อมูลและรหัสผ่าน admin ของแต่ละสาขา — แยกรหัสผ่านจากของแต่ละสาขาโดยเจตนา
    </div>

    <!-- Login -->
    <v-card v-if="!token" class="pa-6" elevation="4">
      <div class="text-subtitle-1 font-weight-medium mb-3">เข้าสู่ระบบ</div>
      <v-form @submit.prevent="login">
        <v-text-field
          v-model="password"
          type="password"
          label="รหัสผ่าน Super Admin"
          variant="outlined"
          density="comfortable"
          autofocus
          class="mb-3"
        />
        <v-btn type="submit" color="primary" block :loading="loginLoading">เข้าสู่ระบบ</v-btn>
      </v-form>
      <v-alert v-if="loginError" type="error" variant="tonal" density="compact" class="mt-4">
        {{ loginError }}
      </v-alert>
    </v-card>

    <!-- Branch management -->
    <template v-else>
      <div class="d-flex align-center justify-space-between mb-4">
        <v-btn variant="text" size="small" @click="logout">ออกจากระบบ</v-btn>
        <v-btn color="primary" prepend-icon="mdi-plus" @click="openNewForm">เพิ่มสาขาใหม่</v-btn>
      </div>

      <v-alert v-if="loadError" type="error" variant="tonal" density="compact" class="mb-4">{{ loadError }}</v-alert>

      <div v-if="loadingBranches" class="text-center py-6">
        <v-progress-circular indeterminate color="primary" />
      </div>

      <v-card v-else elevation="2" class="mb-6">
        <v-list v-if="branches.length > 0" lines="two">
          <template v-for="branch in branches" :key="branch.slug">
            <v-list-item>
              <v-list-item-title>{{ branch.displayName }}</v-list-item-title>
              <v-list-item-subtitle>/{{ branch.slug }}</v-list-item-subtitle>
              <template #append>
                <v-btn variant="text" size="small" icon="mdi-pencil" @click="openEditForm(branch)" />
                <v-btn
                  v-if="confirmDeleteSlug !== branch.slug"
                  variant="text" size="small" icon="mdi-delete" color="error"
                  @click="askDelete(branch.slug)"
                />
                <template v-else>
                  <v-btn variant="text" size="small" color="error" @click="confirmDelete(branch.slug)">ยืนยันลบ</v-btn>
                  <v-btn variant="text" size="small" @click="cancelDelete">ยกเลิก</v-btn>
                </template>
              </template>
            </v-list-item>
            <v-divider />
          </template>
        </v-list>
        <div v-else class="pa-6 text-center text-medium-emphasis">ยังไม่มีสาขาในระบบ — กด "เพิ่มสาขาใหม่" เพื่อเริ่มต้น</div>
      </v-card>
      <v-alert v-if="deleteError" type="error" variant="tonal" density="compact" class="mb-4">{{ deleteError }}</v-alert>

      <!-- Add/edit form -->
      <v-card v-if="editingSlug" class="pa-6" elevation="4">
        <div class="text-subtitle-1 font-weight-medium mb-3">
          {{ isNew ? 'เพิ่มสาขาใหม่' : `แก้ไขสาขา: ${form.displayName}` }}
        </div>
        <v-form @submit.prevent="saveForm">
          <v-text-field
            v-if="isNew"
            v-model="form.slug"
            label="รหัสสาขา (slug) — ใช้ใน URL เช่น /vibharam-sriracha"
            variant="outlined"
            density="comfortable"
            class="mb-3"
            hint="ตัวพิมพ์เล็ก a-z, 0-9, - เท่านั้น — เปลี่ยนไม่ได้ภายหลัง"
            persistent-hint
          />
          <v-text-field
            v-model="form.displayName"
            label="ชื่อโรงพยาบาลที่แสดง"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-text-field
            v-model="form.serverName"
            label="Server Name"
            placeholder="เช่น 192.168.0.88"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-text-field
            v-model="form.databaseName"
            label="Database Name"
            placeholder="เช่น Que_lcb"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-text-field
            v-model="form.dbLogin"
            label="Login"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-text-field
            v-model="form.dbPassword"
            label="Password (DB)"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-switch
            v-model="form.encryptTransfer"
            label="Transfer Mode (เข้ารหัสข้อมูลระหว่างส่ง — Encrypt)"
            color="primary"
            density="comfortable"
            class="mb-3"
            hide-details
          />
          <v-text-field
            v-model="form.adminPassword"
            label="รหัสผ่าน Admin ของสาขานี้"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <div class="d-flex ga-2">
            <v-btn type="submit" color="primary" :loading="formSaving">บันทึก</v-btn>
            <v-btn variant="text" @click="closeForm">ยกเลิก</v-btn>
          </div>
        </v-form>
        <v-alert v-if="formError" type="error" variant="tonal" density="compact" class="mt-4">{{ formError }}</v-alert>
      </v-card>
    </template>
  </v-container>
</template>
