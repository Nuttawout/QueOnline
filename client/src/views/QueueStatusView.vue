<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import confetti from 'canvas-confetti';
import { lookupQueue } from '../api/queue';
import { apiBaseUrl } from '../api/http';
import { useQueueAlert } from '../composables/useQueueAlert';
import { usePushSubscription } from '../composables/usePushSubscription';
import { settings } from '../composables/useSettings';
import QueueStatusCard from '../components/QueueStatusCard.vue';
import { TEMPLATE_COMPONENTS } from '../components/templates/registry';
import { formatWaitMinutes } from '../utils/queueFormat';

// A QR deep-link (?q=... / ?hn=...) triggers an almost-instant entry -> status switch inside
// onMounted, which can race with the entry card's own "appear" transition and leave Vue's
// out-in Transition stuck mid-swap. Only animate the initial appear when we're actually going
// to sit on the entry screen for a moment (no deep-link params).
const hasDeepLinkParams = new URLSearchParams(window.location.search).has('q') ||
  new URLSearchParams(window.location.search).has('hn');

const entryNumber = ref('');
const mode = ref('entry'); // 'entry' | 'status'
const result = ref(null);
const loading = ref(false);
const notFound = ref(false);
const errorMessage = ref('');

// A QR deep-link resolves near-instantly, but sitting on a blank entry form for even a few
// hundred ms while it fetches reads as a stall — a skeleton in the shape of the status card
// gives an immediate "something's happening, here's roughly what you'll see" signal instead.
const showSkeleton = computed(() => hasDeepLinkParams && loading.value && mode.value === 'entry');

function vibrateTap() {
  navigator.vibrate?.(12);
}

const { permission, requestPermission, check, reset: resetAlert, lastAlert } = useQueueAlert();
const push = usePushSubscription();

// Time-of-day greeting + live clock on the entry screen — independent of the admin-configurable
// `settings.greeting` text, shown alongside it rather than replacing it.
const now = ref(new Date());
let clockInterval = null;

function timeOfDayGreeting(date) {
  const hour = date.getHours();
  if (hour >= 5 && hour < 9) return 'สวัสดีตอนเช้าค่ะ';
  if (hour >= 9 && hour < 12) return 'สวัสดีตอนสายๆ ค่ะ';
  if (hour >= 12 && hour < 16) return 'สวัสดีตอนบ่ายค่ะ';
  if (hour >= 16 && hour < 19) return 'สวัสดีตอนเย็นค่ะ';
  if (hour >= 19 && hour < 23) return 'สวัสดีตอนค่ำค่ะ';
  return 'สวัสดีค่ะ'; // ดึก/เช้ามืด
}

const timeGreeting = computed(() => timeOfDayGreeting(now.value));
const formattedTime = computed(() => now.value.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' }));

const showAlertToast = ref(false);
watch(lastAlert, (val) => {
  if (val) showAlertToast.value = true;
});

// null → render the free-form custom (fieldLayout-based) QueueStatusCard, same as before this
// feature existed; any recognized preset id renders that fixed-design component instead.
const activeTemplateComponent = computed(() => TEMPLATE_COMPONENTS[settings.cardTemplate] ?? null);

const arrived = computed(() => result.value?.timeline?.find((s) => s.key === 'call')?.done ?? false);
const aheadCount = computed(() => result.value?.aheadCount ?? null);
const urgency = computed(() => {
  if (arrived.value) return 'arrived';
  if (aheadCount.value !== null && aheadCount.value <= 1) return 'close';
  if (aheadCount.value !== null && aheadCount.value <= 2) return 'near';
  return 'waiting';
});

const formattedWait = computed(() => formatWaitMinutes(result.value?.estimatedWaitMinutes));

// First not-yet-done, not-skipped step is "in progress" (accent-highlighted, matching the
// reference's orange active-step circle) — every step after it is plain "pending" grey. A skipped
// step (never happened, but a later step already did — e.g. no lab test ordered) is excluded here
// so it can't get stuck showing as "in progress" forever; see QueueTimelineStep.Skipped.
const currentStepKey = computed(() => result.value?.timeline?.find((s) => !s.done && !s.skipped)?.key ?? null);

// Animated count instead of the number just snapping to its new value on every 10s refresh.
const displayedAhead = ref(0);
let aheadAnimFrame = null;

function animateAheadTo(target) {
  cancelAnimationFrame(aheadAnimFrame);
  const start = displayedAhead.value;
  if (start === target) return;
  const startTime = performance.now();
  const duration = 500;

  function tick(now) {
    const t = Math.min((now - startTime) / duration, 1);
    const eased = 1 - (1 - t) ** 3; // ease-out cubic
    displayedAhead.value = Math.round(start + (target - start) * eased);
    if (t < 1) aheadAnimFrame = requestAnimationFrame(tick);
  }
  aheadAnimFrame = requestAnimationFrame(tick);
}

function triggerConfetti() {
  confetti({
    particleCount: 120,
    spread: 75,
    origin: { y: 0.55 },
    colors: [settings.primaryColor, '#ffffff', '#4caf50', '#ffd54f'],
  });
}

// Fires once on the false -> true transition, whether that happens on the very first load
// (e.g. a QR scanned after already being called) or partway through polling.
watch(arrived, (val) => {
  if (val) triggerConfetti();
});

let pollHandle = null;
let controller = null;
let queryHn;

function stopPolling() {
  clearInterval(pollHandle);
  pollHandle = null;
}

function checkAlert(data) {
  check({
    aheadCount: data.aheadCount,
    arrived: data.timeline?.find((s) => s.key === 'call')?.done ?? false,
    label: data.qNumber,
  });
}

async function runLookup({ qnumber, hn }) {
  controller?.abort();
  controller = new AbortController();
  loading.value = true;
  errorMessage.value = '';
  notFound.value = false;

  try {
    const data = await lookupQueue({ qnumber, hn }, controller.signal);
    result.value = data;
    displayedAhead.value = Math.max(data.aheadCount, 0); // snap on a fresh search, no count-up from a stale number
    mode.value = 'status';
    checkAlert(data);
    stopPolling();
    pollHandle = setInterval(refresh, 10000);
  } catch (err) {
    if (err?.response?.status === 404) {
      notFound.value = true;
      result.value = null;
      mode.value = 'entry';
    } else if (err?.code !== 'ERR_CANCELED') {
      errorMessage.value = 'ไม่สามารถค้นหาคิวได้ กรุณาลองใหม่';
      mode.value = 'entry';
    }
  } finally {
    loading.value = false;
  }
}

function search() {
  vibrateTap();
  if (!entryNumber.value) {
    errorMessage.value = 'กรอกหมายเลขคิวของท่านก่อนครับ';
    return;
  }
  resetAlert();
  runLookup({ qnumber: entryNumber.value.trim() });
}

async function refresh() {
  if (!result.value) return;
  try {
    const data = await lookupQueue(
      { qnumber: entryNumber.value.trim(), hn: queryHn },
      controller.signal,
    );
    result.value = data;
    animateAheadTo(Math.max(data.aheadCount, 0));
    checkAlert(data);
  } catch {
    // Silent refresh failure — keep showing the last known state.
  }
}

function reset() {
  stopPolling();
  controller?.abort();
  result.value = null;
  entryNumber.value = '';
  notFound.value = false;
  errorMessage.value = '';
  mode.value = 'entry';
  const url = new URL(window.location.href);
  url.search = '';
  window.history.replaceState({}, '', url);
}

function subscribePush() {
  vibrateTap();
  push.subscribe({ hn: queryHn, qnumber: entryNumber.value.trim() });
}

onMounted(() => {
  clockInterval = setInterval(() => {
    now.value = new Date();
  }, 1000);

  const params = new URLSearchParams(window.location.search);
  const q = params.get('q');
  const hn = params.get('hn');

  if (q || hn) {
    entryNumber.value = q ?? '';
    queryHn = hn ?? undefined;
    runLookup({ qnumber: q, hn });
  }
});

onUnmounted(() => {
  stopPolling();
  controller?.abort();
  cancelAnimationFrame(aheadAnimFrame);
  clearInterval(clockInterval);
});
</script>

<template>
  <div class="hero">
    <div class="brand-bar">
      <div v-if="settings.logoUrl" class="logo-badge mr-3">
        <img :src="apiBaseUrl + settings.logoUrl" alt="" class="logo-img" />
      </div>
      <v-avatar v-else color="primary" size="40" class="mr-3">
        <v-icon icon="mdi-hospital-box" color="white" size="26" />
      </v-avatar>
      <div>
        <div class="text-subtitle-1 font-weight-medium brand-name" style="color: rgb(var(--v-theme-primary))">
          {{ settings.hospitalName }}
        </div>
        <div class="text-caption text-medium-emphasis">{{ settings.tagline }}</div>
      </div>
    </div>

    <v-container class="content-container" style="max-width: 480px">
      <Transition name="swap" mode="out-in" :appear="!hasDeepLinkParams">
      <!-- Skeleton — shown only while a QR deep-link is resolving, in the rough shape of the
           status card, so the very first paint already looks like "your queue is loading" rather
           than a blank form. -->
      <v-card v-if="showSkeleton" key="skeleton" class="pa-6 entry-card" elevation="6">
        <v-skeleton-loader type="heading, text, divider, list-item-two-line, list-item-two-line, list-item-two-line" />
      </v-card>

      <!-- Entry screen -->
      <v-card v-else-if="mode === 'entry'" key="entry" class="pa-6 entry-card" elevation="6">
        <div class="text-center mb-3">
          <v-icon icon="mdi-hand-wave-outline" color="primary" size="32" class="mb-1 wave-icon" />
          <div class="text-caption text-medium-emphasis time-greeting">{{ timeGreeting }} • {{ formattedTime }} น.</div>
          <div class="text-h6 font-weight-medium greeting-text">{{ settings.greeting }}</div>
          <div class="text-body-2 text-medium-emphasis mt-1">กรอกหมายเลขคิวของท่านเพื่อดูสถานะ</div>
        </div>

        <v-form @submit.prevent="search">
          <v-text-field
            v-model="entryNumber"
            label="หมายเลขคิวของท่าน"
            placeholder="เช่น A5"
            variant="outlined"
            density="comfortable"
            autofocus
            class="mb-2 queue-input"
            @keyup.enter="search"
          />
          <v-btn type="submit" color="primary" size="large" block :loading="loading" rounded="lg">
            ดูสถานะคิวของฉัน
          </v-btn>
        </v-form>

        <v-alert v-if="notFound" type="warning" variant="tonal" density="compact" class="mt-3">
          ไม่พบหมายเลขคิวนี้ในระบบวันนี้ กรุณาตรวจสอบอีกครั้ง
        </v-alert>
        <v-alert v-if="errorMessage" type="error" variant="tonal" density="compact" class="mt-3">
          {{ errorMessage }}
        </v-alert>
      </v-card>

      <!-- Status screen -->
      <div v-else-if="result" key="status">
        <v-card class="status-card pa-3" :class="`urgency-${urgency}`" elevation="8">
          <component
            :is="activeTemplateComponent"
            v-if="activeTemplateComponent"
            :result="result"
            :arrived="arrived"
            :current-step-key="currentStepKey"
            :displayed-ahead="displayedAhead"
            :formatted-wait="formattedWait"
            :arrived-title="settings.arrivedTitle"
            :arrived-body="settings.arrivedBody"
          />
          <QueueStatusCard
            v-else
            :result="result"
            :arrived="arrived"
            :current-step-key="currentStepKey"
            :displayed-ahead="displayedAhead"
            :formatted-wait="formattedWait"
            :arrived-title="settings.arrivedTitle"
            :arrived-body="settings.arrivedBody"
            :field-layout="settings.fieldLayout"
          />
        </v-card>

        <div class="text-center mt-1">
          <v-btn
            v-if="!push.subscribed.value && !arrived"
            variant="outlined"
            color="primary"
            size="small"
            prepend-icon="mdi-bell-ring-outline"
            :loading="push.loading.value"
            @click="subscribePush"
          >
            แจ้งเตือนแม้ปิดแอป
          </v-btn>
          <div v-else-if="push.subscribed.value" class="text-caption text-success">
            <v-icon size="14" icon="mdi-check-circle" /> เปิดการแจ้งเตือนแบบพุชแล้ว
          </div>
          <div v-if="push.error.value" class="text-caption text-error mt-1">{{ push.error.value }}</div>

          <div
            v-if="permission !== 'granted' && permission !== 'unsupported'"
            class="text-caption text-medium-emphasis mt-2"
          >
            <a href="#" class="text-primary" @click.prevent="requestPermission">เปิดการแจ้งเตือนขณะเปิดหน้านี้ไว้</a>
          </div>

          <div class="mt-2">
            <a href="#" class="text-caption text-medium-emphasis" @click.prevent="reset">ค้นหาคิวอื่น</a>
          </div>
        </div>
      </div>
      </Transition>
    </v-container>

    <v-snackbar v-model="showAlertToast" :timeout="6000" location="bottom" color="primary" multi-line>
      <div class="d-flex align-center ga-2">
        <v-icon icon="mdi-bell-ring" />
        <div>
          <div class="font-weight-medium">{{ lastAlert?.title }}</div>
          <div v-if="lastAlert?.body" class="text-caption">{{ lastAlert.body }}</div>
        </div>
      </div>
      <template #actions>
        <v-btn variant="text" size="small" @click="showAlertToast = false">ปิด</v-btn>
      </template>
    </v-snackbar>
  </div>
</template>

<style scoped>
.hero {
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  background: var(--n-bg);
}

.brand-bar {
  display: flex;
  align-items: center;
  padding: var(--sp-2) var(--sp-5);
  max-width: 480px;
  width: 100%;
  margin: 0 auto;
  background: var(--n-card);
  border-bottom: 1px solid var(--n-border);
  flex-shrink: 0;
}

/* Logos aren't always square (a wide hospital wordmark logo shouldn't get cropped into a circle) —
   a fixed-height pill that the image sizes itself within, instead of a fixed-size circular frame. */
.logo-badge {
  display: flex;
  align-items: center;
  height: 36px;
  max-width: 150px;
  padding: var(--sp-1) var(--sp-2);
}

.logo-img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}

.content-container {
  flex: 1;
  padding-top: var(--sp-2);
  padding-bottom: var(--sp-2);
}

.entry-card,
.status-card {
  border-radius: var(--r-card);
}

.queue-input :deep(input) {
  font-size: var(--fs-lg);
  text-align: center;
  letter-spacing: 0.05em;
  font-weight: var(--fw-medium);
}

.brand-name {
  font-size: calc(var(--fs-base) * var(--text-scale, 1));
  font-weight: var(--fw-medium);
}

.greeting-text {
  font-size: calc(var(--fs-xl) * 0.75 * var(--text-scale, 1));
  font-weight: var(--fw-medium);
}

.time-greeting {
  display: block;
  margin-bottom: var(--sp-1);
}

/* Entry <-> status screen swap */
.swap-enter-active,
.swap-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}

.swap-enter-from {
  opacity: 0;
  transform: scale(0.96) translateY(10px);
}

.swap-leave-to {
  opacity: 0;
  transform: scale(0.96) translateY(-10px);
}

/* Waves a couple of times, then rests before repeating — a real hand-wave rhythm rather
   than a nonstop spin. */
.wave-icon {
  display: inline-block;
  transform-origin: 70% 70%;
  animation: wave 2.5s ease-in-out infinite;
}

@keyframes wave {
  0%,
  60%,
  100% {
    transform: rotate(0deg);
  }
  10% {
    transform: rotate(14deg);
  }
  20% {
    transform: rotate(-8deg);
  }
  30% {
    transform: rotate(14deg);
  }
  40% {
    transform: rotate(-4deg);
  }
  50% {
    transform: rotate(10deg);
  }
}

.bounce-in {
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

@keyframes bounce-in {
  0% {
    transform: scale(0.3);
    opacity: 0;
  }
  60% {
    transform: scale(1.15);
    opacity: 1;
  }
  100% {
    transform: scale(1);
  }
}

@media (prefers-reduced-motion: reduce) {
  .swap-enter-active,
  .swap-leave-active,
  .wave-icon,
  .bounce-in {
    animation: none !important;
    transition: none !important;
  }
}
</style>
