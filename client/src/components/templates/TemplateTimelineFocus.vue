<script setup>
import { formatStepTime, stageWaitText } from '../../utils/queueFormat';

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
</script>

<template>
  <div class="tpl-timeline-focus">
    <div class="strip">
      <div class="strip-top">
        <span class="strip-number">{{ result.qNumber }}</span>
        <div v-if="!arrived" class="strip-chip">
          {{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount} คิว` }}
        </div>
      </div>
      <div v-if="result.patientName" class="strip-name">
        {{ result.patientName }}<template v-if="result.hn"> • HN: {{ result.hn }}</template>
      </div>
    </div>

    <div v-if="arrived" class="arrived-line">
      <span class="arrived-badge">🎉</span>
      <div>
        <div class="arrived-title">{{ arrivedTitle }}</div>
        <div class="arrived-body">{{ arrivedBody }}</div>
      </div>
    </div>

    <div class="timeline">
      <div v-for="(step, i) in steps()" :key="step.key" class="step">
        <div class="step-icon">
          <v-icon v-if="step.done" icon="mdi-check-circle" color="primary" size="28" />
          <v-icon v-else-if="step.skipped" icon="mdi-minus-circle-outline" color="grey-lighten-1" size="28" />
          <v-icon
            v-else-if="step.key === currentStepKey"
            icon="mdi-circle-slice-8"
            size="28"
            class="step-icon-current"
          />
          <v-icon v-else icon="mdi-checkbox-blank-circle-outline" color="grey" size="28" />
          <span v-if="i < steps().length - 1" class="step-stub" :class="step.done ? 'stub-done' : 'stub-pending'" />
        </div>
        <div class="step-content" :class="{ 'step-content-current': step.key === currentStepKey && !step.done }">
          <span class="step-label" :class="{ 'step-skipped': step.skipped }">
            {{ step.label }}<template v-if="step.skipped"> (ข้าม)</template>
          </span>
          <span v-if="step.location" class="step-meta">จุดบริการ: {{ step.location }}</span>
          <span v-if="step.at" class="step-meta">{{ formatStepTime(step.at) }}</span>
          <span v-if="stageWaitText(step)" class="step-meta step-wait">{{ stageWaitText(step) }}</span>
        </div>
      </div>
    </div>

    <div class="footer-line">
      จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
      <template v-if="result.visitNumber"> • เลขที่รับบริการ: {{ result.visitNumber }}</template>
    </div>
  </div>
</template>

<style scoped>
.tpl-timeline-focus {
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
}

.strip {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
  padding-bottom: var(--sp-3);
  border-bottom: 1px solid #edeff3;
}

.strip-top {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--sp-2);
}

.strip-number {
  font-weight: 800;
  font-size: 1.2rem;
  color: var(--accent-color, #e8720f);
  flex-shrink: 0;
}

.strip-name {
  font-size: var(--fs-xs);
  color: #5c6773;
}

.strip-chip {
  flex-shrink: 0;
  background: color-mix(in srgb, var(--accent-color, #e8720f) 12%, white);
  color: color-mix(in srgb, var(--accent-color, #e8720f) 85%, black);
  border-radius: 999px;
  padding: 3px var(--sp-3);
  font-size: var(--fs-xs);
  font-weight: 600;
}

.arrived-line {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
}

.arrived-badge {
  font-size: 1.6rem;
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
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

.timeline {
  display: flex;
  flex-direction: column;
}

.step {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-3);
}

.step-icon {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
}

.step-stub {
  width: 2px;
  flex: 1 1 auto;
  min-height: 22px;
  margin: var(--sp-1) 0;
  border-radius: 1px;
}

.stub-done {
  background: rgb(var(--v-theme-primary));
}

.stub-pending {
  background: #dde1e8;
}

.step-icon-current {
  color: var(--accent-color, #e8720f) !important;
}

.step-content {
  display: flex;
  flex-direction: column;
  padding: 1px 0 var(--sp-4);
}

.step-content-current .step-label {
  color: var(--accent-color, #e8720f);
  font-weight: 600;
}

.step-label {
  font-size: var(--fs-lg);
  font-weight: 500;
  color: #1a2340;
}

.step-skipped {
  color: #9298a3;
  font-weight: 400;
  font-style: italic;
}

.step-meta {
  font-size: var(--fs-sm);
  color: #8891a3;
}

.step-wait {
  color: var(--accent-color, #e8720f);
  font-weight: 500;
}

.footer-line {
  font-size: var(--fs-xs);
  color: #8891a3;
  text-align: center;
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
  .arrived-badge {
    animation: none !important;
  }
}
</style>
