<script setup>
import { computed } from 'vue';
import { formatStepTime, patientLineText, stageWaitText, visitDateText } from '../../utils/queueFormat';
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
  <div class="tpl-neumorphic">
    <div class="badge">
      <span class="n">{{ result.qNumber }}</span>
      <span class="l">คิวของท่าน</span>
    </div>
    <div v-if="patientLine" class="patient-line">{{ patientLine }}</div>
    <div class="meta-time">{{ visitDate }} เวลา {{ liveTimeText }} น.</div>

    <div class="row">
      <template v-if="arrived">
        <div class="pill" style="flex: 1 1 100%;">
          <div class="v" style="color: rgb(var(--v-theme-success))">{{ arrivedTitle }}</div>
          <div class="k">{{ arrivedBody }}</div>
        </div>
      </template>
      <template v-else>
        <div class="pill">
          <div class="v">{{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}</div>
          <div class="k">คิวรอ</div>
        </div>
        <div class="pill">
          <div class="v"><template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">~{{ formattedWait }}</template><template v-else>—</template></div>
          <div class="k">เวลารอ</div>
        </div>
      </template>
    </div>

    <div class="steps">
      <div
        v-for="step in visibleSteps()"
        :key="step.key"
        class="step"
        :class="{ done: step.done, now: step.key === currentStepKey, skipped: step.skipped }"
      >
        <span class="ic">
          <v-icon v-if="step.done" icon="mdi-check" size="12" color="white" />
          <v-icon v-else-if="step.skipped" icon="mdi-minus" size="12" />
        </span>
        {{ step.label }}<template v-if="step.at"> · {{ formatStepTime(step.at) }}</template><template v-else-if="step.key === currentStepKey && stageWaitText(currentStep())"> · {{ stageWaitText(currentStep()) }}</template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-neumorphic {
  margin: -0.75rem;
  background: #e4e8f2;
  color: #333a4d;
  font-family: 'K2D', sans-serif;
  padding: var(--sp-6) var(--sp-5) var(--sp-4);
  border-radius: var(--r-card-lg);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--sp-4);
}

.badge {
  width: 176px; height: 176px; border-radius: 50%;
  background: #e4e8f2;
  box-shadow: 10px 10px 20px #b9bdcb, -10px -10px 20px #ffffff;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
}
.badge .n { font-family: 'Quicksand', sans-serif; font-weight: 700; font-size: 58px; color: #3548e0; }
.badge .l { font-size: var(--fs-sm); color: #7b8196; margin-top: 2px; }
.patient-line { font-size: var(--fs-sm); color: #7b8196; margin-top: -8px; }
.meta-time { font-size: var(--fs-xs); color: #7b8196; font-variant-numeric: tabular-nums; text-align: center; }

.row { display: flex; gap: var(--sp-3); width: 100%; }
.pill { flex: 1; background: #e4e8f2; border-radius: 18px; padding: var(--sp-3); text-align: center; box-shadow: 6px 6px 12px #b9bdcb, -6px -6px 12px #ffffff; }
.pill .v { font-family: 'Quicksand', sans-serif; font-weight: 700; font-size: var(--fs-lg); }
.pill .k { font-size: var(--fs-xs); color: #7b8196; margin-top: 2px; }

.steps { width: 100%; display: flex; flex-direction: column; gap: var(--sp-3); }
.step {
  display: flex; align-items: center; gap: var(--sp-3);
  background: #e4e8f2; border-radius: 14px; padding: var(--sp-3) var(--sp-4); font-size: var(--fs-sm);
  box-shadow: inset 3px 3px 6px #c3c7d4, inset -3px -3px 6px #ffffff;
}
.step.now { box-shadow: 4px 4px 10px #b9bdcb, -4px -4px 10px #ffffff; color: #b64e0f; font-weight: 700; }
.step.skipped { color: #9298a3; font-style: italic; }
.ic { width: 22px; height: 22px; border-radius: 50%; background: #d7dbe6; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.step.now .ic { background: #e8720f; }
.step.done .ic { background: #3548e0; }
</style>
