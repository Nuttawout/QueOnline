<script setup>
import { computed } from 'vue';
import { patientLineText, stageWaitText } from '../../utils/queueFormat';

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

const patientLine = computed(() => patientLineText(props.result, props.showPatientName, props.showHn));
const steps = computed(() => props.result.timeline ?? []);
const currentStep = computed(() => steps.value.find((s) => s.key === props.currentStepKey));
const statusLine = computed(() => {
  if (props.arrived) return arrivedBody(props);
  const step = currentStep.value;
  if (!step) return '—';
  const wait = stageWaitText(step);
  return step.location ? `${step.label} ${step.location}` + (wait ? ` — ${wait}` : '') : step.label + (wait ? ` — ${wait}` : '');
});
function arrivedBody() { return props.arrivedBody; }
</script>

<template>
  <div class="tpl-board">
    <div class="row-top">
      <span>{{ result.departmentName ?? result.groupName ?? '—' }}</span>
      <span class="now">NOW SERVING <b>{{ result.currentServingNumber ?? '—' }}</b></span>
    </div>

    <div class="hero">
      <div class="lbl">หมายเลขคิวของท่าน</div>
      <div class="num">{{ result.qNumber }}</div>
      <div v-if="patientLine" class="patient-line">{{ patientLine }}</div>
    </div>
    <hr class="rule" />

    <div class="board">
      <template v-if="!arrived">
        <div class="line">
          <span class="k">คิวรอ</span><span class="dots"></span>
          <span class="v amber">{{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount} คิว` }}</span>
        </div>
        <div class="line">
          <span class="k">เวลารอโดยประมาณ</span><span class="dots"></span>
          <span class="v">
            <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">~{{ formattedWait }}</template>
            <template v-else>—</template>
          </span>
        </div>
      </template>
      <div v-else class="line">
        <span class="k">สถานะ</span><span class="dots"></span>
        <span class="v amber">{{ arrivedTitle }}</span>
      </div>
      <div class="line">
        <span class="k">จุดบริการ</span><span class="dots"></span>
        <span class="v">{{ result.departmentName ?? result.groupName ?? '—' }}</span>
      </div>
      <div class="line">
        <span class="k">เลขที่รับบริการ</span><span class="dots"></span>
        <span class="v">{{ result.visitNumber ?? '—' }}</span>
      </div>
    </div>

    <div class="status"><span class="cursor"></span>{{ statusLine }}</div>

    <div class="segs">
      <div
        v-for="step in steps"
        :key="step.key"
        class="seg"
        :class="{ done: step.done || step.skipped, current: step.key === currentStepKey }"
      ></div>
    </div>
  </div>
</template>

<style scoped>
.tpl-board {
  margin: -0.75rem;
  background: #14171c;
  color: #f2e9d8;
  font-family: 'Chakra Petch', sans-serif;
  padding: var(--sp-5) var(--sp-4);
  border-radius: var(--r-card-lg);
  display: flex;
  flex-direction: column;
}

.row-top {
  display: flex; justify-content: space-between; align-items: center;
  font-size: var(--fs-sm); letter-spacing: 0.08em; text-transform: uppercase;
  color: #d99a3a; margin-bottom: var(--sp-4);
}
.row-top .now b { color: #f2e9d8; font-size: var(--fs-base); letter-spacing: 0; margin-left: 6px; }

.hero { text-align: center; padding: var(--sp-1) 0 var(--sp-3); }
.hero .lbl { font-size: var(--fs-sm); letter-spacing: 0.14em; text-transform: uppercase; color: #8b8f98; margin-bottom: 6px; }
.patient-line { font-size: var(--fs-sm); color: #9aa0ab; margin-top: 4px; }
.hero .num {
  font-family: 'Big Shoulders Display', sans-serif;
  font-weight: 900; font-size: 120px; line-height: 0.9;
  color: #f2e9d8;
}

.rule { border: none; border-top: 1px solid #3a3f49; margin: 6px 0 var(--sp-4); }

.board { display: flex; flex-direction: column; gap: var(--sp-3); font-family: 'Sarabun', sans-serif; }
.line { display: flex; align-items: baseline; gap: 6px; font-size: var(--fs-base); }
.k { color: #9aa0ab; white-space: nowrap; }
.dots { flex: 1; border-bottom: 1px dotted #454b57; margin-bottom: 4px; }
.v { color: #f2e9d8; font-weight: 600; white-space: nowrap; font-variant-numeric: tabular-nums; }
.v.amber { color: #d99a3a; }

.status {
  margin-top: var(--sp-5); font-size: var(--fs-sm); color: #d99a3a;
  display: flex; align-items: center; gap: var(--sp-2);
}
.status .cursor {
  width: 8px; height: 15px; background: #d99a3a; flex-shrink: 0;
  animation: blink 1s step-end infinite;
}

.segs { display: flex; gap: 4px; margin-top: auto; padding-top: var(--sp-5); }
.seg { flex: 1; height: 5px; border-radius: 2px; background: #2b2f38; }
.seg.done { background: #6b7280; }
.seg.current { background: #d99a3a; }

@keyframes blink { 50% { opacity: 0; } }

@media (prefers-reduced-motion: reduce) {
  .status .cursor { animation: none !important; }
}
</style>
