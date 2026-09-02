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
  <div class="tpl-ticket">
    <div class="stub">
      <div class="queue-label">คิวของท่าน</div>
      <div class="queue-number">{{ result.qNumber }}</div>
      <div v-if="!arrived" class="stub-wait">
        <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">
          อีก {{ formattedWait }}
        </template>
      </div>
      <div v-else class="stub-arrived">🎉</div>
    </div>

    <div class="perforation" />

    <div class="content">
      <div class="patient-line">
        {{ result.patientName
        }}<template v-if="result.patientName && result.hn"> • HN: {{ result.hn }}</template>
      </div>

      <div v-if="arrived" class="arrived-block">
        <div class="arrived-title">{{ arrivedTitle }}</div>
        <div class="arrived-body">{{ arrivedBody }}</div>
      </div>
      <div v-else class="stats-line">
        <span>เลขคิวปัจจุบัน <b>{{ result.currentServingNumber ?? '—' }}</b></span>
        <span>{{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไปคือคุณ' : `อีก ${displayedAhead ?? result.aheadCount} คิว` }}</span>
      </div>

      <div class="stage-track">
        <template v-for="(step, i) in steps()" :key="step.key">
          <div class="stage-dot-wrap">
            <span
              class="stage-dot"
              :class="{ done: step.done, current: step.key === currentStepKey, skipped: step.skipped }"
            />
          </div>
          <span v-if="i < steps().length - 1" class="stage-line" :class="{ done: step.done || step.skipped }" />
        </template>
      </div>
      <div class="stage-current-label">{{ currentStepLabel() }}</div>
      <div v-if="stageWaitText(currentStep())" class="stage-wait">{{ stageWaitText(currentStep()) }}</div>

      <div class="footer-line">
        จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
        <template v-if="result.visitNumber"> • เลขที่: {{ result.visitNumber }}</template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-ticket {
  display: flex;
  align-items: stretch;
  min-height: 15rem;
  margin: -0.75rem;
  border-radius: var(--r-card-lg);
  overflow: hidden;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04), 0 12px 28px rgba(0, 0, 0, 0.08);
}

.stub {
  flex: 0 0 32%;
  background: linear-gradient(165deg, rgb(var(--v-theme-primary)), color-mix(in srgb, rgb(var(--v-theme-primary)) 80%, black));
  color: white;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--sp-1);
  padding: var(--sp-3) var(--sp-1);
  text-align: center;
}

.queue-label {
  font-size: var(--fs-xs);
  letter-spacing: 0.04em;
  opacity: 0.8;
}

.queue-number {
  font-size: 2.3rem;
  font-weight: 800;
  line-height: 1;
  color: white;
}

.stub-wait {
  font-size: var(--fs-xs);
  opacity: 0.85;
  min-height: 1em;
}

.stub-arrived {
  font-size: 1.7rem;
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.perforation {
  flex: 0 0 16px;
  position: relative;
}

.perforation::before {
  content: '';
  position: absolute;
  top: 8px;
  bottom: 8px;
  left: 50%;
  width: 0;
  border-left: 2px dashed color-mix(in srgb, rgb(var(--v-theme-primary)) 25%, white);
}

.content {
  flex: 1 1 auto;
  background: white;
  display: flex;
  flex-direction: column;
  gap: var(--sp-3);
  padding: var(--sp-4) var(--sp-4) var(--sp-3) var(--sp-4);
  min-width: 0;
}

.patient-line {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.arrived-title {
  color: rgb(var(--v-theme-success));
  font-weight: 600;
  font-size: var(--fs-lg);
}

.arrived-body {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.stats-line {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
  font-size: var(--fs-sm);
  color: #1a2340;
}

.stage-track {
  display: flex;
  align-items: center;
  margin-top: var(--sp-1);
}

.stage-dot-wrap {
  display: flex;
  align-items: center;
}

.stage-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: #e3e7ee;
  flex-shrink: 0;
}

.stage-dot.done {
  background: rgb(var(--v-theme-primary));
}

.stage-dot.current {
  background: var(--accent-color, #e8720f);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent-color, #e8720f) 25%, transparent);
}

.stage-dot.skipped {
  background: #c7cbd3;
  border: 2px dashed #9298a3;
}

.stage-line {
  flex: 1 1 auto;
  height: 2px;
  background: #e3e7ee;
  min-width: 6px;
}

.stage-line.done {
  background: rgb(var(--v-theme-primary));
}

.stage-current-label {
  font-size: var(--fs-sm);
  font-weight: 600;
  color: var(--accent-color, #e8720f);
}

.stage-wait {
  font-size: var(--fs-xs);
  color: #8891a3;
}

.footer-line {
  margin-top: auto;
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
  .stub-arrived {
    animation: none !important;
  }
}
</style>
