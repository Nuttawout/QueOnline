<script setup>
import { computed } from 'vue';
import { formatStepTime, patientLineText } from '../../utils/queueFormat';

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

const visibleSteps = () => props.result.timeline ?? [];
function stepTime(step) {
  if (step.done && step.at) return formatStepTime(step.at);
  if (step.skipped) return 'ข้าม';
  if (step.key === props.currentStepKey) return props.arrived ? 'เสร็จสิ้น' : 'กำลังทำ';
  return '—';
}
</script>

<template>
  <div class="tpl-swiss">
    <div class="eyebrow"><span>{{ result.departmentName ?? result.groupName ?? '—' }}</span><span>{{ result.visitNumber ? `เลขที่ ${result.visitNumber}` : '' }}</span></div>
    <div class="num">{{ result.qNumber }}</div>
    <div v-if="patientLine" class="name">{{ patientLine }}</div>

    <div class="grid">
      <div class="cell">
        <div class="k">คิวปัจจุบัน</div>
        <div class="v">{{ result.currentServingNumber ?? '—' }}</div>
      </div>
      <div class="cell">
        <div class="k">คิวรอ</div>
        <div class="v accent">{{ (result.aheadCount ?? 0) <= 0 ? '0' : (displayedAhead ?? result.aheadCount) }}</div>
      </div>
      <div class="cell">
        <div class="k">เวลารอ</div>
        <div class="v">
          <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">{{ formattedWait }}</template>
          <template v-else>—</template>
        </div>
      </div>
      <div class="cell">
        <div class="k">สถานะ</div>
        <div class="v" style="font-size:15px">{{ arrived ? arrivedTitle : 'กำลังรอ' }}</div>
      </div>
    </div>

    <div class="list">
      <div
        v-for="(step, i) in visibleSteps()"
        :key="step.key"
        class="li"
        :class="{ now: step.key === currentStepKey && !step.done }"
      >
        <span><span class="idx">{{ String(i + 1).padStart(2, '0') }}</span>{{ step.label }}<template v-if="step.skipped"> (ข้าม)</template></span>
        <span class="t">{{ stepTime(step) }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-swiss {
  margin: -0.75rem;
  background: #fafafa;
  color: #0e0f12;
  font-family: 'Archivo', 'Noto Sans Thai', sans-serif;
  padding: var(--sp-6) var(--sp-5) var(--sp-4);
  border-radius: var(--r-card-lg);
}

.eyebrow { font-size: var(--fs-sm); letter-spacing: 0.1em; text-transform: uppercase; color: #6b6d73; border-bottom: 1px solid #0e0f12; padding-bottom: var(--sp-3); display: flex; justify-content: space-between; }
.num { font-family: 'Archivo', sans-serif; font-weight: 900; font-size: 112px; line-height: 0.9; margin: var(--sp-4) 0 4px; letter-spacing: -0.02em; }
.name { font-size: var(--fs-sm); color: #6b6d73; margin-bottom: var(--sp-5); }

.grid { display: grid; grid-template-columns: 1fr 1fr; }
.cell { border-top: 1px solid #d8d9dd; padding: var(--sp-3) 0; }
.cell:nth-child(odd) { padding-right: var(--sp-3); }
.k { font-size: var(--fs-xs); text-transform: uppercase; letter-spacing: 0.06em; color: #8a8c92; }
.v { font-size: var(--fs-lg); font-weight: 700; margin-top: 2px; font-variant-numeric: tabular-nums; }
.v.accent { color: #3548e0; }

.list { margin-top: var(--sp-5); border-top: 1px solid #0e0f12; padding-top: var(--sp-4); display: flex; flex-direction: column; gap: var(--sp-3); }
.li { display: flex; justify-content: space-between; font-size: var(--fs-sm); }
.idx { color: #b3b4b9; font-variant-numeric: tabular-nums; margin-right: var(--sp-2); }
.li.now { font-weight: 700; }
.t { color: #b3b4b9; font-variant-numeric: tabular-nums; }
.li.now .t { color: #3548e0; }
</style>
