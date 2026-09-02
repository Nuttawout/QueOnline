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

const visibleSteps = () => props.result.timeline ?? [];
</script>

<template>
  <div class="tpl-dark">
    <div class="header">
      <div class="queue-label">หมายเลขคิวของท่าน</div>
      <div class="queue-number">{{ result.qNumber }}</div>
      <div class="patient-line">
        {{ result.patientName
        }}<template v-if="result.patientName && result.hn"> • HN: {{ result.hn }}</template>
      </div>
    </div>

    <div v-if="arrived" class="arrived">
      <div class="arrived-badge">🎉</div>
      <div class="arrived-title">{{ arrivedTitle }}</div>
      <div class="arrived-body">{{ arrivedBody }}</div>
    </div>
    <template v-else>
      <div class="stats-row">
        <div class="stat-box">
          <div class="stat-label">คิวปัจจุบัน</div>
          <div class="stat-value">{{ result.currentServingNumber ?? '—' }}</div>
        </div>
        <div class="stat-box">
          <div class="stat-label">คิวรอ</div>
          <div class="stat-value">
            {{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}
          </div>
        </div>
      </div>
      <div v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined" class="wait-line">
        รออีกประมาณ <b>{{ formattedWait }}</b>
      </div>
    </template>

    <div class="timeline">
      <div v-for="(step, i) in visibleSteps()" :key="step.key" class="step">
        <div class="step-icon">
          <span class="dot" :class="{ done: step.done, current: step.key === currentStepKey, skipped: step.skipped }">
            <v-icon v-if="step.done" icon="mdi-check" size="12" color="white" />
            <v-icon v-else-if="step.skipped" icon="mdi-minus" size="12" color="white" />
          </span>
          <span v-if="i < visibleSteps().length - 1" class="stub" :class="{ done: step.done }" />
        </div>
        <div class="step-content">
          <span
            class="step-label"
            :class="{ 'label-current': step.key === currentStepKey && !step.done, 'step-skipped': step.skipped }"
          >
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
.tpl-dark {
  margin: -0.75rem;
  padding: 1.1rem 1rem;
  /* Fixed dark neutral surface — a "dark mode" needs a real dark background regardless of the
     hospital's brand hue (mixing an arbitrary light brand color down to "dark" tends to look
     muddy), so this one surface tone is the deliberate exception to the "derive everything from
     primary/accent" rule the other templates follow. Brand identity still shows through: the
     queue number and active-step accents below use the admin's actual accent/primary colors. */
  background: #12182a;
  border-radius: 20px;
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  text-align: center;
  color: #e8ecf5;
}

.header {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
}

.queue-label {
  font-size: var(--fs-xs);
  letter-spacing: 0.04em;
  color: #6b7488;
}

.queue-number {
  font-size: 2.75rem;
  font-weight: 800;
  line-height: 1;
  color: var(--accent-color, #e8720f);
  text-shadow: 0 0 26px color-mix(in srgb, var(--accent-color, #e8720f) 60%, transparent);
}

.patient-line {
  font-size: var(--fs-sm);
  color: #8891a8;
}

.arrived-badge {
  font-size: 2.2rem;
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.arrived-title {
  color: color-mix(in srgb, rgb(var(--v-theme-success)) 65%, white);
  font-weight: 600;
  font-size: var(--fs-lg);
}

.arrived-body {
  font-size: var(--fs-sm);
  color: #c3c9d9;
}

.stats-row {
  display: flex;
  gap: var(--sp-2);
  justify-content: center;
}

.stat-box {
  flex: 1;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: var(--r-control);
  padding: var(--sp-2) var(--sp-3);
}

.stat-label {
  font-size: var(--fs-xs);
  color: #6b7488;
}

.stat-value {
  font-weight: 600;
  font-size: var(--fs-lg);
  color: #f2f4f9;
}

.wait-line {
  font-size: var(--fs-sm);
  color: #c3c9d9;
}

.timeline {
  display: flex;
  flex-direction: column;
  text-align: left;
}

.step {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-2);
}

.step-icon {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
}

.dot {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: 2px;
}

.dot.done {
  background: rgb(var(--v-theme-primary));
}

.dot.current {
  background: var(--accent-color, #e8720f);
  box-shadow: 0 0 0 4px color-mix(in srgb, var(--accent-color, #e8720f) 30%, transparent);
}

.dot.skipped {
  background: #454e63;
}

.stub {
  width: 2px;
  height: 17px;
  margin: var(--sp-1) 0;
  border-radius: 1px;
  background: rgba(255, 255, 255, 0.1);
}

.stub.done {
  background: rgb(var(--v-theme-primary));
}

.step-content {
  display: flex;
  flex-direction: column;
  padding: 2px 0 var(--sp-2);
}

.step-label {
  font-size: var(--fs-base);
  font-weight: 500;
  color: #e8ecf5;
}

.label-current {
  color: var(--accent-color, #e8720f);
  font-weight: 600;
}

.step-skipped {
  color: #6b7488;
  font-weight: 400;
  font-style: italic;
}

.step-meta {
  font-size: var(--fs-xs);
  color: #6b7488;
}

.step-wait {
  color: var(--accent-color, #e8720f);
  font-weight: 500;
}

.footer-line {
  font-size: var(--fs-xs);
  color: #6b7488;
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
