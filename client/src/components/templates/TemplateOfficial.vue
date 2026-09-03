<script setup>
import { computed } from 'vue';
import { formatStepTime, visitDateText } from '../../utils/queueFormat';
import { useLiveClock } from '../../composables/useLiveClock';

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
function statusText(step) {
  if (step.done) return `เสร็จสิ้น${step.at ? ` (${formatStepTime(step.at)})` : ''}`;
  if (step.skipped) return 'ไม่มีในการรับบริการนี้';
  if (step.key === props.currentStepKey) return props.arrived ? 'เสร็จสิ้น' : 'กำลังดำเนินการ';
  return 'รอดำเนินการ';
}

const liveNow = useLiveClock();
const liveTimeText = computed(() => liveNow.value.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
const visitDate = computed(() => visitDateText(props.result));
</script>

<template>
  <div class="tpl-official">
    <div class="crest">⚕</div>
    <div class="title">ใบแจ้งสถานะคิวตรวจ</div>
    <div class="sub">{{ result.departmentName ?? result.groupName ?? '—' }}</div>
    <hr class="rule" />

    <table class="table">
      <tbody>
        <tr><td>วันที่มารับบริการ</td><td>{{ visitDate }}</td></tr>
        <tr><td>เวลาปัจจุบัน</td><td class="clock">{{ liveTimeText }} น.</td></tr>
        <tr v-if="showPatientName"><td>ชื่อผู้ป่วย</td><td>{{ result.patientName ?? '—' }}</td></tr>
        <tr v-if="showHn && result.hn"><td>เลขประจำตัวผู้ป่วย (HN)</td><td>{{ result.hn }}</td></tr>
        <tr><td>คิวที่กำลังให้บริการ</td><td>{{ result.currentServingNumber ?? '—' }}</td></tr>
        <tr><td>จำนวนคิวก่อนหน้า</td><td>{{ (result.aheadCount ?? 0) <= 0 ? 0 : (displayedAhead ?? result.aheadCount) }}</td></tr>
        <tr>
          <td>เวลารอโดยประมาณ</td>
          <td><template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">{{ formattedWait }}</template><template v-else>—</template></td>
        </tr>
      </tbody>
    </table>

    <div class="num-row">
      <div class="l">หมายเลขคิวของท่าน</div>
      <div class="n">{{ result.qNumber }}</div>
    </div>

    <div v-if="arrived" class="arrived-note">{{ arrivedTitle }} — {{ arrivedBody }}</div>

    <div class="list">
      <div
        v-for="(step, i) in visibleSteps()"
        :key="step.key"
        class="li"
        :class="{ done: step.done || step.key === currentStepKey, pending: !step.done && step.key !== currentStepKey }"
      >
        <span class="no">{{ i + 1 }}.</span>{{ step.label }} — {{ statusText(step) }}
      </div>
    </div>

    <div class="stamp">ระบบติดตามคิวออนไลน์ · ออกเอกสารอัตโนมัติ</div>
  </div>
</template>

<style scoped>
.tpl-official {
  margin: -0.75rem;
  background: #f8f4ea;
  color: #1f2a44;
  font-family: 'Sarabun', sans-serif;
  padding: var(--sp-6) var(--sp-5) var(--sp-4);
  border-radius: var(--r-card-lg);
  display: flex;
  flex-direction: column;
}

.crest { width: 40px; height: 40px; border-radius: 50%; border: 2px solid #a6822e; margin: 0 auto var(--sp-3); display: flex; align-items: center; justify-content: center; font-size: var(--fs-lg); color: #a6822e; }
.title { text-align: center; font-weight: 700; font-size: var(--fs-lg); }
.sub { text-align: center; font-size: var(--fs-sm); color: #6b7280; margin-top: 2px; }
.rule { border: none; border-top: 1.5px solid #1f2a44; border-bottom: 1px solid #1f2a44; height: 3px; margin: var(--sp-4) 0 var(--sp-5); }

.table { width: 100%; border-collapse: collapse; font-size: var(--fs-sm); }
.table td { padding: var(--sp-2) 4px; border-bottom: 1px dotted #cbd0da; }
.table td:first-child { color: #6b7280; width: 45%; }
.table td:last-child { font-weight: 700; text-align: right; }
.table td.clock { font-variant-numeric: tabular-nums; }

.num-row { text-align: center; margin: var(--sp-5) 0; padding: var(--sp-4); border: 1.5px solid #a6822e; border-radius: 4px; }
.num-row .l { font-size: var(--fs-sm); color: #8a6f24; letter-spacing: 0.08em; }
.num-row .n { font-family: 'Sarabun', sans-serif; font-weight: 700; font-size: 60px; color: #1f2a44; }

.arrived-note { text-align: center; font-weight: 700; color: rgb(var(--v-theme-success)); margin-bottom: var(--sp-4); font-size: var(--fs-base); }

.list { font-size: var(--fs-sm); display: flex; flex-direction: column; gap: var(--sp-2); }
.li { display: flex; gap: 6px; }
.li .no { color: #a6822e; font-weight: 700; width: 16px; flex-shrink: 0; }
.li.done { color: #1f2a44; }
.li.pending { color: #9aa1af; }

.stamp { margin-top: auto; padding-top: var(--sp-4); text-align: right; font-size: var(--fs-xs); color: #8a1f2b; font-style: italic; }
</style>
