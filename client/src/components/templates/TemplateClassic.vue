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
  <div class="tpl-classic">
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
    <div v-else class="stats-row">
      <div class="stat-box">
        <div class="stat-label">เลขคิวปัจจุบัน</div>
        <div class="stat-value">{{ result.currentServingNumber ?? '—' }}</div>
      </div>
      <div class="stat-box">
        <div class="stat-label">จำนวนคิวรอ</div>
        <div class="stat-value">
          {{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount} คิว` }}
        </div>
      </div>
    </div>
    <div v-if="!arrived" class="wait-box">
      <div class="stat-label">ระยะเวลารอโดยประมาณ</div>
      <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">
        <div class="wait-value">อีกประมาณ {{ formattedWait }}</div>
      </template>
      <div v-else class="wait-fallback">ยังไม่มีข้อมูลเพียงพอสำหรับประมาณเวลาวันนี้</div>
    </div>

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
          <span v-if="i < visibleSteps().length - 1" class="step-stub" :class="step.done ? 'stub-done' : 'stub-pending'" />
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

    <div class="footer">
      <div class="footer-line">
        จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
        <template v-if="result.visitNumber"> • เลขที่รับบริการ: {{ result.visitNumber }}</template>
      </div>
      <div class="footer-strong">หมายเลขคิวของคุณ: {{ result.qNumber }}</div>
    </div>
  </div>
</template>

<style scoped>
.tpl-classic {
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
  font-size: 44px;
  font-weight: 800;
  line-height: 1;
  letter-spacing: -0.01em;
  color: var(--accent-color, #e8720f);
  text-shadow: 0 2px 10px color-mix(in srgb, var(--accent-color, #e8720f) 22%, transparent);
}

.patient-line {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.arrived {
  padding: var(--sp-2) 0;
}

.arrived-badge {
  font-size: 2.5rem;
  line-height: 1;
  margin-bottom: var(--sp-1);
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.arrived-title {
  color: rgb(var(--v-theme-success));
  font-weight: 500;
  font-size: var(--fs-lg);
  margin-bottom: var(--sp-1);
}

.arrived-body {
  font-size: var(--fs-base);
  color: #5c6773;
}

.stats-row {
  display: flex;
  gap: var(--sp-2);
  justify-content: center;
}

.stat-box,
.wait-box {
  background: color-mix(in srgb, rgb(var(--v-theme-primary)) 5%, white);
  border: 1px solid color-mix(in srgb, rgb(var(--v-theme-primary)) 9%, white);
  border-radius: var(--r-control);
  padding: var(--sp-2) var(--sp-4);
  flex: 1;
}

.wait-box {
  max-width: 100%;
}

.stat-label {
  font-size: var(--fs-xs);
  color: #8891a3;
}

.stat-value,
.wait-value {
  font-weight: 600;
  font-size: var(--fs-lg);
  color: #1a2340;
}

.wait-fallback {
  font-size: var(--fs-sm);
  color: #5c6773;
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

.step-stub {
  width: 2px;
  height: 18px;
  margin-top: var(--sp-1);
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

.footer {
  text-align: left;
  font-size: var(--fs-xs);
  color: #8891a3;
  padding-top: var(--sp-2);
  border-top: 1px solid #edeff3;
}

.footer-strong {
  font-weight: 500;
  color: #5c6773;
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
