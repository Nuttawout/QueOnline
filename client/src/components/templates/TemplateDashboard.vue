<script setup>
import { computed } from 'vue';
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

const steps = computed(() => props.result.timeline ?? []);
// A skipped step (no lab test ordered, etc.) counts toward progress same as a done one — both mean
// "resolved", just not "not started yet" — otherwise progress would permanently undercount for any
// visit that skips a stage.
const resolvedCount = computed(() => steps.value.filter((s) => s.done || s.skipped).length);
const progressPct = computed(() => (steps.value.length ? Math.round((resolvedCount.value / steps.value.length) * 100) : 0));
const currentStep = computed(
  () => steps.value.find((s) => s.key === props.currentStepKey)
    ?? (props.arrived ? null : steps.value.find((s) => !s.done && !s.skipped)),
);
const currentStepLabel = computed(() => currentStep.value?.label ?? (props.arrived ? 'ถึงคิวแล้ว' : '—'));
</script>

<template>
  <div class="tpl-dashboard">
    <div class="tile header-tile">
      <div>
        <div class="queue-label">หมายเลขคิวของท่าน</div>
        <div class="queue-number">{{ result.qNumber }}</div>
      </div>
      <div class="patient-line">
        {{ result.patientName
        }}<template v-if="result.patientName && result.hn"><br />HN: {{ result.hn }}</template>
      </div>
    </div>

    <div v-if="arrived" class="tile arrived-tile">
      <span class="arrived-badge">🎉</span>
      <div>
        <div class="arrived-title">{{ arrivedTitle }}</div>
        <div class="arrived-body">{{ arrivedBody }}</div>
      </div>
    </div>
    <div v-else class="tile-row">
      <div class="tile stat-tile">
        <div class="stat-label">คิวปัจจุบัน</div>
        <div class="stat-value">{{ result.currentServingNumber ?? '—' }}</div>
      </div>
      <div class="tile stat-tile">
        <div class="stat-label">คิวรอ</div>
        <div class="stat-value">
          {{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}
        </div>
      </div>
      <div class="tile stat-tile">
        <div class="stat-label">เวลารอ</div>
        <div class="stat-value stat-value-sm">
          <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">
            {{ formattedWait }}
          </template>
          <template v-else>—</template>
        </div>
      </div>
    </div>

    <div class="tile progress-tile">
      <div class="progress-track">
        <div class="progress-fill" :style="{ width: `${progressPct}%` }" />
      </div>
      <div class="progress-label">
        <span>{{ currentStepLabel }}</span>
        <span class="progress-pct">{{ progressPct }}%</span>
      </div>
      <div v-if="stageWaitText(currentStep)" class="stage-wait">{{ stageWaitText(currentStep) }}</div>
    </div>

    <div class="footer-line">
      จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
      <template v-if="result.visitNumber"> • เลขที่รับบริการ: {{ result.visitNumber }}</template>
    </div>
  </div>
</template>

<style scoped>
.tpl-dashboard {
  display: flex;
  flex-direction: column;
  gap: var(--sp-2);
}

.tile {
  background: color-mix(in srgb, rgb(var(--v-theme-primary)) 4%, white);
  border: 1px solid color-mix(in srgb, rgb(var(--v-theme-primary)) 8%, white);
  border-radius: var(--r-control);
  padding: var(--sp-3);
}

.header-tile {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-2);
  background: linear-gradient(135deg, color-mix(in srgb, rgb(var(--v-theme-primary)) 8%, white), color-mix(in srgb, var(--accent-color, #e8720f) 6%, white));
}

.queue-label {
  font-size: var(--fs-xs);
  color: #8891a3;
}

.queue-number {
  font-size: 2rem;
  font-weight: 800;
  line-height: 1;
  color: var(--accent-color, #e8720f);
}

.patient-line {
  font-size: var(--fs-xs);
  color: #5c6773;
  text-align: right;
}

.tile-row {
  display: flex;
  gap: var(--sp-2);
}

.stat-tile {
  flex: 1;
  text-align: center;
}

.stat-label {
  font-size: var(--fs-xs);
  color: #8891a3;
}

.stat-value {
  font-weight: 600;
  font-size: var(--fs-lg);
  margin-top: 2px;
  color: #1a2340;
}

.stat-value-sm {
  font-size: var(--fs-base);
}

.arrived-tile {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  background: color-mix(in srgb, rgb(var(--v-theme-success)) 8%, white);
  border-color: color-mix(in srgb, rgb(var(--v-theme-success)) 16%, white);
}

.arrived-badge {
  font-size: 1.8rem;
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

.progress-track {
  height: 8px;
  border-radius: 999px;
  background: #e3e7ee;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, rgb(var(--v-theme-primary)), var(--accent-color, #e8720f));
  border-radius: inherit;
  transition: width 0.4s ease;
}

.progress-label {
  display: flex;
  justify-content: space-between;
  font-size: var(--fs-sm);
  font-weight: 500;
  margin-top: var(--sp-1);
  color: #1a2340;
}

.stage-wait {
  font-size: var(--fs-xs);
  color: #8891a3;
  margin-top: 2px;
}

.progress-pct {
  color: #8891a3;
  font-weight: 400;
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
