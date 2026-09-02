<script setup>
import { computed } from 'vue';
import { formatStepTime, patientLineText, stageWaitText } from '../../utils/queueFormat';

const props = defineProps({
  result: { type: Object, required: true },
  arrived: { type: Boolean, default: false },
  currentStepKey: { type: String, default: null },
  displayedAhead: { type: Number, default: null },
  formattedWait: { type: String, default: '' },
  arrivedTitle: { type: String, default: 'ถึงคิวของท่านแล้ว' },
  arrivedBody: { type: String, default: 'กรุณาเข้ารับบริการ' },
  showPatientName: { type: Boolean, default: true },
  showHn: { type: Boolean, default: true },
});

const visibleSteps = () => props.result.timeline ?? [];
const currentStep = () => visibleSteps().find((s) => s.key === props.currentStepKey);
const patientLine = computed(() => patientLineText(props.result, props.showPatientName, props.showHn));
</script>

<template>
  <div class="tpl-boarding">
    <div class="head">
      <div class="row1"><span>คิวออนไลน์</span><span>Boarding Pass</span></div>
      <div class="row2">
        <div>
          <div class="num-label">หมายเลขคิวของท่าน</div>
          <div class="num">{{ result.qNumber }}</div>
        </div>
        <div class="hospital-block">
          <div class="hospital">{{ result.departmentName ?? result.groupName ?? '—' }}</div>
        </div>
      </div>
      <div v-if="patientLine" class="patient">{{ patientLine }}</div>
    </div>

    <div class="perf"></div>
    <div class="dash"></div>

    <div class="body">
      <template v-if="arrived">
        <div class="arrived">
          <div class="badge">🎉</div>
          <div class="arrived-title">{{ arrivedTitle }}</div>
          <div class="arrived-body">{{ arrivedBody }}</div>
        </div>
      </template>
      <div v-else class="grid">
        <div class="field">
          <div class="k">คิวปัจจุบัน</div>
          <div class="v">{{ result.currentServingNumber ?? '—' }}</div>
        </div>
        <div class="field">
          <div class="k">คิวรอ</div>
          <div class="v accent">
            {{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}
          </div>
        </div>
        <div class="field">
          <div class="k">เวลารอโดยประมาณ</div>
          <div class="v">
            <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">~{{ formattedWait }}</template>
            <template v-else>—</template>
          </div>
        </div>
        <div class="field">
          <div class="k">เลขที่รับบริการ</div>
          <div class="v">{{ result.visitNumber ?? '—' }}</div>
        </div>
      </div>

      <div class="route">
        <div
          v-for="step in visibleSteps()"
          :key="step.key"
          class="stop"
          :class="{ done: step.done, current: step.key === currentStepKey, skipped: step.skipped }"
        >
          <span class="line"></span>
          <span class="dot"></span>
          <span class="lbl">{{ step.label }}<template v-if="step.skipped"> (ข้าม)</template></span>
        </div>
      </div>
      <div v-if="stageWaitText(currentStep())" class="stage-wait">{{ stageWaitText(currentStep()) }}</div>

      <div class="barcode"></div>
      <div class="foot">
        <span>QUEONLINE · {{ result.qNumber }}</span>
        <span>{{ formatStepTime(visibleSteps().find((s) => s.done)?.at) }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-boarding {
  margin: -0.75rem;
  background: #fbfaf7;
  font-family: 'IBM Plex Sans Thai', sans-serif;
  border-radius: var(--r-card-lg);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.head {
  background: #0a1f3d;
  color: #f4f6fb;
  padding: var(--sp-5) var(--sp-5) var(--sp-6);
}

.row1 {
  display: flex;
  justify-content: space-between;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: #7f93bd;
  margin-bottom: var(--sp-3);
}

.row2 {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
}

.num-label {
  font-size: var(--fs-sm);
  color: #9aacd1;
  margin-bottom: 2px;
}

.num {
  font-family: 'IBM Plex Mono', monospace;
  font-weight: 700;
  font-size: 78px;
  line-height: 1;
}

.hospital {
  font-size: var(--fs-sm);
  color: #9aacd1;
  text-align: right;
  max-width: 120px;
}

.patient {
  margin-top: var(--sp-2);
  font-size: var(--fs-sm);
  color: #c3ceea;
}

.perf {
  position: relative;
  height: 0;
}
.perf::before, .perf::after {
  content: '';
  position: absolute;
  top: -10px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: var(--n-bg);
}
.perf::before { left: -10px; }
.perf::after { right: -10px; }
.dash {
  border-top: 2px dashed #c7cdd8;
  margin: 0 20px;
}

.body {
  padding: var(--sp-5);
  display: flex;
  flex-direction: column;
  gap: var(--sp-4);
}

.grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-4) var(--sp-3);
}

.k {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 9.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: #9aa1af;
  margin-bottom: 3px;
}

.v {
  font-size: var(--fs-lg);
  font-weight: 700;
  color: #14213d;
}

.v.accent { color: #c85a12; }

.arrived {
  text-align: center;
  padding: var(--sp-2) 0;
}

.badge {
  font-size: 2.4rem;
  animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.arrived-title {
  color: rgb(var(--v-theme-success));
  font-weight: 700;
  font-size: var(--fs-xl);
  margin-top: var(--sp-1);
}

.arrived-body {
  font-size: var(--fs-base);
  color: #5c6773;
}

.route {
  display: flex;
  align-items: flex-start;
}

.stop {
  flex: 1;
  text-align: center;
  position: relative;
}

.stop .dot {
  display: block;
  width: 11px;
  height: 11px;
  border-radius: 50%;
  margin: 0 auto 6px;
  background: #d8dce3;
}

.stop.done .dot { background: #14213d; }
.stop.current .dot { background: #c85a12; box-shadow: 0 0 0 4px rgba(200, 90, 18, 0.18); }
.stop.skipped .dot { background: #c7cbd3; border: 2px dashed #9298a3; }

.stop .line {
  position: absolute;
  top: 5px;
  left: -50%;
  width: 100%;
  height: 2px;
  background: #e2e5ea;
  z-index: -1;
}
.stop:first-child .line { display: none; }
.stop.done .line, .stop.current .line { background: #14213d; }

.stop .lbl {
  font-size: var(--fs-xs);
  color: #6b7280;
  line-height: 1.3;
}
.stop.current .lbl { color: #c85a12; font-weight: 700; }
.stop.skipped .lbl { color: #9298a3; font-style: italic; }

.stage-wait {
  text-align: center;
  font-size: var(--fs-sm);
  color: #c85a12;
  font-weight: 600;
  margin-top: -8px;
}

.barcode {
  height: 30px;
  background: repeating-linear-gradient(90deg, #14213d 0 2px, transparent 2px 5px, #14213d 5px 6px, transparent 6px 10px);
  opacity: 0.85;
}

.foot {
  display: flex;
  justify-content: space-between;
  font-size: 10px;
  color: #9aa1af;
  font-family: 'IBM Plex Mono', monospace;
}

@keyframes bounce-in {
  0% { transform: scale(0.3); opacity: 0; }
  60% { transform: scale(1.15); opacity: 1; }
  100% { transform: scale(1); }
}

@media (prefers-reduced-motion: reduce) {
  .badge { animation: none !important; }
}
</style>
