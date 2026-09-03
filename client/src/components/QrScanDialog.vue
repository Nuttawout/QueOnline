<script setup>
import { ref, watch, nextTick, onBeforeUnmount } from 'vue';
import QrScanner from 'qr-scanner';
import QrScannerWorkerPath from 'qr-scanner/qr-scanner-worker.min.js?url';

QrScanner.WORKER_PATH = QrScannerWorkerPath;

const props = defineProps({
  modelValue: { type: Boolean, default: false },
});
const emit = defineEmits(['update:modelValue', 'detected']);

const videoEl = ref(null);
const state = ref('starting'); // 'starting' | 'scanning' | 'denied' | 'unsupported'
let scanner = null;

async function startScanner() {
  state.value = 'starting';
  await nextTick();

  if (!QrScanner.hasCamera || !navigator.mediaDevices?.getUserMedia) {
    state.value = 'unsupported';
    return;
  }

  scanner = new QrScanner(
    videoEl.value,
    (result) => onDetected(result.data),
    { preferredCamera: 'environment', highlightScanRegion: true, highlightCodeOutline: true },
  );

  try {
    await scanner.start();
    state.value = 'scanning';
  } catch {
    state.value = 'denied';
  }
}

function stopScanner() {
  scanner?.stop();
  scanner?.destroy();
  scanner = null;
}

function onDetected(text) {
  stopScanner();
  emit('detected', text);
  emit('update:modelValue', false);
}

function close() {
  emit('update:modelValue', false);
}

watch(
  () => props.modelValue,
  (open) => {
    if (open) startScanner();
    else stopScanner();
  },
);

onBeforeUnmount(stopScanner);
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="420"
    @update:model-value="(v) => emit('update:modelValue', v)"
  >
    <v-card class="pa-4 scan-card" rounded="lg">
      <div class="d-flex align-center justify-space-between mb-3">
        <div class="text-subtitle-1 font-weight-medium">สแกน QR คิวของท่าน</div>
        <v-btn icon="mdi-close" variant="text" size="small" @click="close" />
      </div>

      <div class="scan-viewport">
        <video ref="videoEl" class="scan-video" playsinline muted />
        <div v-if="state === 'starting'" class="scan-overlay">
          <v-progress-circular indeterminate color="primary" size="32" />
        </div>
        <div v-else-if="state === 'denied'" class="scan-overlay text-center pa-4">
          <v-icon icon="mdi-camera-off-outline" size="32" color="error" class="mb-2" />
          <div class="text-body-2">ไม่สามารถเข้าถึงกล้องได้ กรุณาอนุญาตการใช้กล้องในเบราว์เซอร์ แล้วลองใหม่</div>
        </div>
        <div v-else-if="state === 'unsupported'" class="scan-overlay text-center pa-4">
          <v-icon icon="mdi-camera-off-outline" size="32" color="warning" class="mb-2" />
          <div class="text-body-2">อุปกรณ์นี้ไม่รองรับการสแกนด้วยกล้อง กรุณากรอกหมายเลขคิวแทน</div>
        </div>
      </div>

      <div class="text-caption text-medium-emphasis text-center mt-3">
        เล็งกล้องไปที่ QR code บนบัตรคิวของท่าน
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.scan-card {
  background: var(--n-card);
}

.scan-viewport {
  position: relative;
  width: 100%;
  aspect-ratio: 1 / 1;
  border-radius: var(--r-control);
  overflow: hidden;
  background: #000;
}

.scan-video {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.scan-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.55);
  color: #fff;
}
</style>
