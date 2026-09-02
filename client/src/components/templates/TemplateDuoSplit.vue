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
</script>

<template>
  <div class="tpl-duo">
    <div class="side" :class="{ 'side-arrived': arrived }">
      <div class="side-label">คิวของท่าน</div>
      <div class="side-number">{{ result.qNumber }}</div>
      <div v-if="arrived" class="side-badge">🎉</div>
      <div v-else-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined" class="side-wait">
        อีกประมาณ<br />{{ formattedWait }}
      </div>
    </div>

    <div class="content">
      <div class="patient-line">
        {{ result.patientName
        }}<template v-if="result.patientName && result.hn"> • HN: {{ result.hn }}</template>
      </div>

      <div v-if="arrived" class="arrived-block">
        <div class="arrived-title">{{ arrivedTitle }}</div>
        <div class="arrived-body">{{ arrivedBody }}</div>
      </div>
      <div v-else class="stats-row">
        <span>คิวปัจจุบัน <b>{{ result.currentServingNumber ?? '—' }}</b></span>
        <span>{{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไปคือคุณ' : `อีก ${displayedAhead ?? result.aheadCount} คิว` }}</span>
      </div>

      <div class="timeline">
        <div v-for="(step, i) in visibleSteps()" :key="step.key" class="step">
          <div class="step-icon">
            <v-icon v-if="step.done" icon="mdi-check-circle" color="primary" size="18" />
            <v-icon v-else-if="step.skipped" icon="mdi-minus-circle-outline" color="grey-lighten-1" size="18" />
            <v-icon
              v-else-if="step.key === currentStepKey"
              icon="mdi-circle-slice-8"
              size="18"
              class="step-icon-current"
            />
            <v-icon v-else icon="mdi-checkbox-blank-circle-outline" color="grey" size="18" />
            <span v-if="i < visibleSteps().length - 1" class="stub" :class="{ done: step.done }" />
          </div>
          <span class="step-content">
            <span
              class="step-label"
              :class="{ 'label-current': step.key === currentStepKey && !step.done, 'step-skipped': step.skipped }"
            >
              {{ step.label }}<template v-if="step.skipped"> (ข้าม)</template>
            </span>
            <span v-if="stageWaitText(step)" class="step-wait">{{ stageWaitText(step) }}</span>
          </span>
        </div>
      </div>

      <div class="footer-line">
        จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
        <template v-if="result.visitNumber"> • เลขที่: {{ result.visitNumber }}</template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-duo {
  display: flex;
  align-items: stretch;
  min-height: 15.5rem;
  margin: -0.75rem -0.75rem;
}

.side {
  flex: 0 0 34%;
  background: linear-gradient(170deg, rgb(var(--v-theme-primary)), color-mix(in srgb, rgb(var(--v-theme-primary)) 78%, black));
  color: white;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--sp-1);
  padding: var(--sp-4) var(--sp-2);
  text-align: center;
  border-radius: 0 var(--r-card-lg) var(--r-card-lg) 0;
}

.side-arrived {
  background: linear-gradient(170deg, rgb(var(--v-theme-success)), color-mix(in srgb, rgb(var(--v-theme-success)) 78%, black));
}

.side-label {
  font-size: var(--fs-xs);
  letter-spacing: 0.03em;
  opacity: 0.85;
}

.side-number {
  font-size: 2.4rem;
  font-weight: 800;
  line-height: 1;
}

.side-wait {
  font-size: var(--fs-xs);
  opacity: 0.9;
  line-height: 1.3;
}

.side-badge {
  font-size: 1.7rem;
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.content {
  flex: 1 1 auto;
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
  font-size: var(--fs-base);
}

.arrived-body {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.stats-row {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
  font-size: var(--fs-sm);
  color: #1a2340;
}

.timeline {
  display: flex;
  flex-direction: column;
}

.step {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-2);
}

.step-content {
  display: flex;
  flex-direction: column;
}

.step-icon {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
}

.stub {
  width: 2px;
  height: 10px;
  background: #dde1e8;
  border-radius: 1px;
}

.stub.done {
  background: rgb(var(--v-theme-primary));
}

.step-icon-current {
  color: var(--accent-color, #e8720f) !important;
}

.step-label {
  font-size: var(--fs-sm);
  font-weight: 500;
  color: #1a2340;
  padding: var(--sp-1) 0;
}

.label-current {
  color: var(--accent-color, #e8720f);
  font-weight: 600;
}

.step-wait {
  font-size: var(--fs-xs);
  color: var(--accent-color, #e8720f);
  font-weight: 500;
}

.step-skipped {
  color: #9298a3;
  font-style: italic;
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
  .side-badge {
    animation: none !important;
  }
}
</style>
