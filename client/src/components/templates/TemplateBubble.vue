<script setup>
import { stageWaitText } from '../../utils/queueFormat';

const props = defineProps({
  result: { type: Object, required: true },
  arrived: { type: Boolean, default: false },
  currentStepKey: { type: String, default: null },
  displayedAhead: { type: Number, default: null },
  formattedWait: { type: String, default: '' },
  arrivedTitle: { type: String, default: 'ถึงคิวของท่านแล้ว' },
  arrivedBody: { type: String, default: 'กรุณาเข้ารับบริการ' },
});

const visibleSteps = () => props.result.timeline ?? [];
const currentStep = () => visibleSteps().find((s) => s.key === props.currentStepKey);
</script>

<template>
  <div class="tpl-bubble">
    <div class="badge" :class="{ 'badge-arrived': arrived }">
      <span v-if="arrived" class="badge-emoji">🎉</span>
      <span v-else class="badge-number">{{ result.qNumber }}</span>
    </div>
    <div class="queue-label">หมายเลขคิวของท่าน</div>
    <div class="patient-pill">
      {{ result.patientName
      }}<template v-if="result.patientName && result.hn"> • HN: {{ result.hn }}</template>
    </div>

    <div v-if="arrived" class="arrived-block">
      <div class="arrived-title">{{ arrivedTitle }}</div>
      <div class="arrived-body">{{ arrivedBody }}</div>
    </div>
    <div v-else class="pill-row">
      <div class="pill">
        <span class="pill-label">คิวปัจจุบัน</span>
        <span class="pill-value">{{ result.currentServingNumber ?? '—' }}</span>
      </div>
      <div class="pill">
        <span class="pill-label">คิวรอ</span>
        <span class="pill-value">
          {{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}
        </span>
      </div>
      <div v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined" class="pill">
        <span class="pill-label">เวลารอ</span>
        <span class="pill-value">{{ formattedWait }}</span>
      </div>
    </div>

    <div class="stage-pills">
      <div
        v-for="step in visibleSteps()"
        :key="step.key"
        class="stage-pill"
        :class="{ done: step.done, current: step.key === currentStepKey, skipped: step.skipped }"
      >
        <v-icon v-if="step.done" icon="mdi-check" size="12" color="white" />
        <v-icon v-else-if="step.skipped" icon="mdi-minus" size="12" />
        {{ step.label }}<template v-if="step.skipped"> (ข้าม)</template>
      </div>
    </div>
    <div v-if="stageWaitText(currentStep())" class="stage-wait">{{ stageWaitText(currentStep()) }}</div>

    <div class="footer-line">
      จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
      <template v-if="result.visitNumber"> • เลขที่รับบริการ: {{ result.visitNumber }}</template>
    </div>
  </div>
</template>

<style scoped>
.tpl-bubble {
  margin: -0.75rem;
  padding: var(--sp-5) var(--sp-4) var(--sp-4);
  background: linear-gradient(165deg, color-mix(in srgb, var(--accent-color, #e8720f) 10%, white), color-mix(in srgb, rgb(var(--v-theme-primary)) 6%, white));
  border-radius: var(--r-card-lg);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--sp-2);
  text-align: center;
}

.badge {
  width: 92px;
  height: 92px;
  border-radius: 50%;
  background: linear-gradient(160deg, var(--accent-color, #e8720f), color-mix(in srgb, var(--accent-color, #e8720f) 78%, black));
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 10px 20px -4px color-mix(in srgb, var(--accent-color, #e8720f) 50%, transparent);
  border: 4px solid white;
}

.badge-arrived {
  background: linear-gradient(160deg, rgb(var(--v-theme-success)), color-mix(in srgb, rgb(var(--v-theme-success)) 78%, black));
  box-shadow: 0 10px 20px -4px color-mix(in srgb, rgb(var(--v-theme-success)) 50%, transparent);
}

.badge-number {
  font-size: 2rem;
  font-weight: 800;
}

.badge-emoji {
  font-size: 2.4rem;
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.queue-label {
  font-size: var(--fs-xs);
  color: #8a7266;
}

.patient-pill {
  background: rgba(255, 255, 255, 0.75);
  border-radius: 999px;
  padding: 3px var(--sp-3);
  font-size: var(--fs-xs);
  color: #5c6773;
}

.arrived-title {
  color: rgb(var(--v-theme-success));
  font-weight: 600;
  font-size: var(--fs-base);
}

.arrived-body {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.pill-row {
  display: flex;
  gap: var(--sp-2);
  flex-wrap: wrap;
  justify-content: center;
}

.pill {
  display: flex;
  flex-direction: column;
  background: white;
  border-radius: 999px;
  padding: var(--sp-1) var(--sp-4);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.pill-label {
  font-size: 10px;
  color: #a3a9b7;
}

.pill-value {
  font-weight: 600;
  font-size: var(--fs-sm);
  color: #1a2340;
}

.stage-pills {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-1);
  justify-content: center;
  margin-top: var(--sp-1);
}

.stage-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border-radius: 999px;
  padding: 5px var(--sp-3);
  font-size: var(--fs-xs);
  background: white;
  color: #a3a9b7;
  border: 1px solid #f1f0ee;
}

.stage-pill.done {
  background: rgb(var(--v-theme-primary));
  color: white;
  border-color: transparent;
}

.stage-pill.current {
  background: color-mix(in srgb, var(--accent-color, #e8720f) 16%, white);
  color: color-mix(in srgb, var(--accent-color, #e8720f) 85%, black);
  font-weight: 600;
  border-color: var(--accent-color, #e8720f);
}

.stage-pill.skipped {
  color: #9298a3;
  font-style: italic;
  border-style: dashed;
}

.footer-line {
  font-size: var(--fs-xs);
  color: #a3937f;
  margin-top: var(--sp-1);
}

.stage-wait {
  font-size: var(--fs-xs);
  font-weight: 600;
  color: color-mix(in srgb, var(--accent-color, #e8720f) 85%, black);
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
  .badge-emoji {
    animation: none !important;
  }
}
</style>
