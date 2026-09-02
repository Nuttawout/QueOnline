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

const steps = () => props.result.timeline ?? [];
const currentStep = () => steps().find((s) => s.key === props.currentStepKey)
  ?? (props.arrived ? null : steps().find((s) => !s.done && !s.skipped));
const currentStepLabel = () => currentStep()?.label ?? (props.arrived ? 'ถึงคิวแล้ว' : '—');
</script>

<template>
  <div class="tpl-hero">
    <div class="banner" :class="{ 'banner-arrived': arrived }">
      <template v-if="arrived">
        <div class="banner-badge">🎉</div>
        <div class="banner-arrived-title">{{ arrivedTitle }}</div>
        <div class="banner-arrived-body">{{ arrivedBody }}</div>
      </template>
      <template v-else>
        <div class="queue-label">หมายเลขคิวของท่าน</div>
        <div class="queue-number">{{ result.qNumber }}</div>
      </template>
    </div>

    <div class="body">
      <div class="patient-line">
        {{ result.patientName
        }}<template v-if="result.patientName && result.hn"> • HN: {{ result.hn }}</template>
      </div>

      <div v-if="!arrived" class="pill-row">
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

      <div class="stage-row">
        <template v-for="(step, i) in steps()" :key="step.key">
          <div
            class="stage-circle"
            :class="{ done: step.done, current: step.key === currentStepKey, skipped: step.skipped }"
          >
            <v-icon v-if="step.done" icon="mdi-check" size="14" color="white" />
            <v-icon v-else-if="step.skipped" icon="mdi-minus" size="14" color="white" />
            <span v-else>{{ i + 1 }}</span>
          </div>
          <div v-if="i < steps().length - 1" class="stage-connector" :class="{ done: step.done || step.skipped }" />
        </template>
      </div>
      <div class="stage-current-label">{{ currentStepLabel() }}</div>
      <div v-if="stageWaitText(currentStep())" class="stage-wait">{{ stageWaitText(currentStep()) }}</div>

      <div class="footer-line">
        จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
        <template v-if="result.visitNumber"> • เลขที่รับบริการ: {{ result.visitNumber }}</template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-hero {
  display: flex;
  flex-direction: column;
  /* Bleeds the banner edge-to-edge by canceling the parent .status-card's `pa-3` padding
     (Vuetify spacing unit = 4px, so pa-3 = 12px = 0.75rem) — only safe because this template
     always renders as that card's sole child. */
  margin: -0.75rem -0.75rem 0;
}

.banner {
  background: linear-gradient(160deg, rgb(var(--v-theme-primary)), color-mix(in srgb, rgb(var(--v-theme-primary)) 75%, black));
  color: white;
  padding: var(--sp-5) var(--sp-4) 34px;
  border-radius: 0 0 var(--r-card-lg) var(--r-card-lg);
  text-align: center;
  box-shadow: 0 14px 26px -12px color-mix(in srgb, rgb(var(--v-theme-primary)) 55%, transparent);
}

.banner-arrived {
  background: linear-gradient(160deg, rgb(var(--v-theme-success)), color-mix(in srgb, rgb(var(--v-theme-success)) 75%, black));
  box-shadow: 0 14px 26px -12px color-mix(in srgb, rgb(var(--v-theme-success)) 55%, transparent);
}

.queue-label {
  font-size: var(--fs-xs);
  letter-spacing: 0.04em;
  opacity: 0.85;
}

.queue-number {
  font-size: 3.2rem;
  font-weight: 800;
  line-height: 1.1;
}

.banner-badge {
  font-size: 2.2rem;
  line-height: 1;
  margin-bottom: var(--sp-1);
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.banner-arrived-title {
  font-weight: 600;
  font-size: var(--fs-lg);
}

.banner-arrived-body {
  font-size: var(--fs-sm);
  opacity: 0.9;
}

.body {
  display: flex;
  flex-direction: column;
  gap: var(--sp-4);
  padding: var(--sp-4) var(--sp-3) var(--sp-2);
  text-align: center;
}

.patient-line {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.pill-row {
  display: flex;
  gap: var(--sp-2);
  justify-content: center;
  flex-wrap: wrap;
}

.pill {
  display: flex;
  flex-direction: column;
  background: color-mix(in srgb, rgb(var(--v-theme-primary)) 6%, white);
  border: 1px solid color-mix(in srgb, rgb(var(--v-theme-primary)) 10%, white);
  border-radius: 999px;
  padding: var(--sp-1) var(--sp-4);
}

.pill-label {
  font-size: 10px;
  opacity: 0.65;
}

.pill-value {
  font-weight: 600;
  font-size: var(--fs-base);
  color: #1a2340;
}

.stage-row {
  display: flex;
  align-items: center;
  justify-content: center;
}

.stage-circle {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: #eceff4;
  color: #8891a3;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--fs-xs);
  font-weight: 600;
  flex-shrink: 0;
}

.stage-circle.done {
  background: rgb(var(--v-theme-primary));
  color: white;
}

.stage-circle.current {
  background: var(--accent-color, #e8720f);
  color: white;
  box-shadow: 0 0 0 4px color-mix(in srgb, var(--accent-color, #e8720f) 20%, transparent);
}

.stage-circle.skipped {
  background: #c7cbd3;
}

.stage-current-label {
  font-size: var(--fs-sm);
  font-weight: 600;
  color: var(--accent-color, #e8720f);
  margin-top: var(--sp-1);
}

.stage-wait {
  font-size: var(--fs-xs);
  color: #8891a3;
}

.stage-connector {
  flex: 1 1 auto;
  height: 2px;
  background: #eceff4;
  min-width: 4px;
}

.stage-connector.done {
  background: rgb(var(--v-theme-primary));
}

.footer-line {
  font-size: var(--fs-xs);
  color: #8891a3;
  padding-top: var(--sp-2);
  border-top: 1px solid #edeff3;
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
  .banner-badge {
    animation: none !important;
  }
}
</style>
