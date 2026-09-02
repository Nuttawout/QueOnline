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
  <div class="tpl-gradient">
    <div class="wash">
      <div class="wash-label">หมายเลขคิวของท่าน</div>
      <div class="wash-number">{{ result.qNumber }}</div>
    </div>

    <div class="panel">
      <div class="patient-line">
        {{ result.patientName
        }}<template v-if="result.patientName && result.hn"> • HN: {{ result.hn }}</template>
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
.tpl-gradient {
  margin: -0.75rem -0.75rem 0;
  /* No padding-bottom here on purpose — .panel below must be the last thing this wrapper shows.
     It used to carry var(--sp-4) of bottom padding, which left a gap under .panel's negative-
     margin overlap where the raw gradient (orange, at that corner) showed through as a stray
     band under the card. */
  background:
    radial-gradient(120% 140% at 15% -10%, color-mix(in srgb, white 30%, transparent), transparent 55%),
    linear-gradient(135deg, rgb(var(--v-theme-primary)), var(--accent-color, #e8720f));
  border-radius: 0 0 var(--r-card-lg) var(--r-card-lg);
}

.wash {
  text-align: center;
  color: white;
  padding: var(--sp-5) var(--sp-4) 38px;
}

.wash-label {
  font-size: var(--fs-xs);
  letter-spacing: 0.04em;
  opacity: 0.88;
}

.wash-number {
  font-size: 2.9rem;
  font-weight: 800;
  line-height: 1;
}

.panel {
  background: white;
  border-radius: var(--r-card) var(--r-card) 0 0;
  box-shadow: 0 -6px 20px rgba(0, 0, 0, 0.1);
  /* Flush to the wrapper's sides and bottom (only pulled up over .wash at the top) — an inset on
     the sides here left the raw gradient visible as a full-height stripe down both edges, most
     noticeable near the bottom-right corner where the diagonal gradient trends orange. */
  margin: -30px 0 0;
  padding: var(--sp-4);
  display: flex;
  flex-direction: column;
  gap: var(--sp-3);
  text-align: center;
}

.patient-line {
  font-size: var(--fs-sm);
  color: #5c6773;
}

.arrived-badge {
  font-size: 2rem;
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
  background: color-mix(in srgb, var(--accent-color, #e8720f) 8%, white);
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

.dot {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: #eceff4;
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
  box-shadow: 0 0 0 4px color-mix(in srgb, var(--accent-color, #e8720f) 22%, transparent);
}

.dot.skipped {
  background: #c7cbd3;
}

.stub {
  width: 2px;
  height: 17px;
  margin: var(--sp-1) 0;
  border-radius: 1px;
  background: #eceff4;
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
  font-size: var(--fs-sm);
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
  font-size: var(--fs-xs);
  color: #8891a3;
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
