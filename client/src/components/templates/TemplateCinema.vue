<script setup>
import { computed } from 'vue';
import { patientLineText, stageWaitText, visitDateText } from '../../utils/queueFormat';
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
const currentStep = () => visibleSteps().find((s) => s.key === props.currentStepKey);
const patientLine = computed(() => patientLineText(props.result, props.showPatientName, props.showHn));

const liveNow = useLiveClock();
const liveTimeText = computed(() => liveNow.value.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
const visitDate = computed(() => visitDateText(props.result));
</script>

<template>
  <div class="tpl-cinema">
    <div class="eyebrow"><span>{{ result.departmentName ?? result.groupName ?? '—' }}</span><span>ตั๋วคิว</span></div>
    <div class="seat-lbl">หมายเลขคิวของท่าน</div>
    <div class="num">{{ result.qNumber }}</div>
    <div v-if="patientLine" class="name">{{ patientLine }}</div>
    <div class="meta-time">{{ visitDate }} เวลา {{ liveTimeText }} น.</div>
    <div class="dash"></div>

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
        <div class="v pink">{{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}</div>
      </div>
      <div class="field">
        <div class="k">เวลารอ</div>
        <div class="v">
          <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">~{{ formattedWait }}</template>
          <template v-else>—</template>
        </div>
      </div>
      <div class="field">
        <div class="k">จุดบริการ</div>
        <div class="v" style="font-size:16px">{{ currentStep()?.location || currentStep()?.label || '—' }}</div>
      </div>
    </div>
    <div v-if="!arrived && stageWaitText(currentStep())" class="stage-wait">{{ stageWaitText(currentStep()) }}</div>

    <div class="strip">
      <div
        v-for="step in visibleSteps()"
        :key="step.key"
        class="dot"
        :class="{ done: step.done || step.skipped, now: step.key === currentStepKey }"
      ></div>
    </div>
  </div>
</template>

<style scoped>
.tpl-cinema {
  margin: -0.75rem;
  background: radial-gradient(120% 100% at 20% 0%, #2c1a52, #1b1030 60%);
  color: #f3ecff;
  font-family: 'Prompt', sans-serif;
  padding: var(--sp-6) var(--sp-5) var(--sp-5);
  clip-path: polygon(0 0, 100% 0, 100% 100%, 92% 96%, 84% 100%, 76% 96%, 68% 100%, 60% 96%, 52% 100%, 44% 96%, 36% 100%, 28% 96%, 20% 100%, 12% 96%, 4% 100%, 0 96%);
}

.eyebrow { font-size: var(--fs-sm); letter-spacing: 0.12em; text-transform: uppercase; color: #c9a6ff; display: flex; justify-content: space-between; }
.seat-lbl { font-size: var(--fs-sm); color: #c9a6ff; margin-top: var(--sp-5); }
.num { font-family: 'Bebas Neue', sans-serif; font-size: 110px; line-height: 0.9; color: #fff; letter-spacing: 0.02em; text-shadow: 0 0 24px rgba(255, 61, 129, 0.55); }
.name { font-size: var(--fs-sm); color: #cabfe0; margin-top: var(--sp-2); }
.meta-time { font-size: var(--fs-xs); color: #a996c9; margin-top: 3px; font-variant-numeric: tabular-nums; }
.dash { border-top: 2px dashed rgba(243, 236, 255, 0.25); margin: var(--sp-5) 0; }

.arrived { text-align: center; padding: var(--sp-3) 0 var(--sp-4); }
.badge { font-size: 2.4rem; animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1); }
.arrived-title { color: #ff9fc2; font-weight: 700; font-size: var(--fs-xl); margin-top: var(--sp-1); }
.arrived-body { font-size: var(--fs-base); color: #cabfe0; }

.grid { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }
.k { font-size: var(--fs-xs); color: #a996c9; text-transform: uppercase; letter-spacing: 0.06em; }
.v { font-family: 'Bebas Neue', sans-serif; font-size: 22px; color: #fff; letter-spacing: 0.02em; }
.v.pink { color: #ff3d81; }

.stage-wait { margin-top: var(--sp-2); font-size: var(--fs-sm); color: #ff3d81; font-weight: 600; }

.strip { display: flex; justify-content: space-between; margin-top: var(--sp-5); }
.dot { width: 9px; height: 9px; border-radius: 50%; background: rgba(243, 236, 255, 0.25); }
.dot.done { background: #ff3d81; }
.dot.now { background: #fff; box-shadow: 0 0 0 4px rgba(255, 61, 129, 0.35); }

@keyframes bounce-in {
  0% { transform: scale(0.3); opacity: 0; }
  60% { transform: scale(1.15); opacity: 1; }
  100% { transform: scale(1); }
}

@media (prefers-reduced-motion: reduce) {
  .badge { animation: none !important; }
}
</style>
