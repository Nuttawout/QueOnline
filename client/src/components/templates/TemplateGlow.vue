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
  <div class="tpl-glow" :class="{ 'tpl-glow-arrived': arrived }">
    <div class="frame">
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
            <v-icon v-if="step.done" icon="mdi-check-circle" color="primary" size="20" />
            <v-icon v-else-if="step.skipped" icon="mdi-minus-circle-outline" color="grey-lighten-1" size="20" />
            <v-icon
              v-else-if="step.key === currentStepKey"
              icon="mdi-circle-slice-8"
              size="20"
              class="step-icon-current"
            />
            <v-icon v-else icon="mdi-checkbox-blank-circle-outline" color="grey" size="20" />
            <span v-if="i < visibleSteps().length - 1" class="stub" :class="{ done: step.done }" />
          </div>
          <div class="step-content">
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
  </div>
</template>

<style scoped>
.tpl-glow {
  margin: -0.75rem;
  padding: 3px;
  border-radius: 20px;
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), var(--accent-color, #e8720f));
  box-shadow: 0 0 26px color-mix(in srgb, var(--accent-color, #e8720f) 35%, transparent);
}

.tpl-glow-arrived {
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-success)));
  box-shadow: 0 0 26px color-mix(in srgb, rgb(var(--v-theme-success)) 40%, transparent);
}

.frame {
  background: white;
  border-radius: 17px;
  padding: var(--sp-4) var(--sp-4);
  display: flex;
  flex-direction: column;
  gap: var(--sp-4);
  text-align: center;
}

.header {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
}

.queue-label {
  font-size: var(--fs-xs);
  letter-spacing: 0.03em;
  color: #8891a3;
}

.queue-number {
  font-size: 2.6rem;
  font-weight: 800;
  line-height: 1;
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), var(--accent-color, #e8720f));
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.patient-line {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.arrived-badge {
  font-size: 2.2rem;
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
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

.stats-row {
  display: flex;
  gap: var(--sp-2);
  justify-content: center;
}

.stat-box {
  flex: 1;
  background: linear-gradient(135deg, color-mix(in srgb, rgb(var(--v-theme-primary)) 6%, white), color-mix(in srgb, var(--accent-color, #e8720f) 5%, white));
  border-radius: var(--r-control);
  padding: var(--sp-2) var(--sp-3);
}

.stat-label {
  font-size: var(--fs-xs);
  color: #8891a3;
}

.stat-value {
  font-weight: 600;
  font-size: var(--fs-base);
  color: #1a2340;
}

.wait-line {
  font-size: var(--fs-sm);
  color: #1a2340;
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

.stub {
  width: 2px;
  height: 18px;
  margin-top: var(--sp-1);
  border-radius: 1px;
  background: #dde1e8;
}

.stub.done {
  background: rgb(var(--v-theme-primary));
}

.step-icon-current {
  color: var(--accent-color, #e8720f) !important;
}

.step-content {
  display: flex;
  flex-direction: column;
  padding: 1px 0 var(--sp-3);
}

.step-label {
  font-size: var(--fs-base);
  font-weight: 500;
  color: #1a2340;
}

.step-skipped {
  color: #9298a3;
  font-weight: 400;
  font-style: italic;
}

.step-meta {
  font-size: var(--fs-xs);
  color: #8891a3;
}

.step-wait {
  color: var(--accent-color, #e8720f);
  font-weight: 500;
}

.footer-line {
  text-align: left;
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
  .arrived-badge {
    animation: none !important;
  }
}
</style>
