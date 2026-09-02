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

const visibleSteps = () => props.result.timeline ?? [];
const currentStep = () => visibleSteps().find((s) => s.key === props.currentStepKey);
const patientLine = computed(() => patientLineText(props.result, props.showPatientName, props.showHn));
</script>

<template>
  <div class="tpl-restaurant">
    <div class="head">
      <div class="icon">🏥</div>
      <div class="h">{{ result.departmentName ?? result.groupName ?? 'ระบบคิวออนไลน์' }}</div>
      <div class="s">คิวของท่าน</div>
    </div>

    <div class="card">
      <div class="num-lbl">หมายเลขคิว</div>
      <div class="num">{{ result.qNumber }}</div>
      <div v-if="patientLine" class="name">{{ patientLine }}</div>

      <template v-if="arrived">
        <div class="arrived">
          <div class="badge">🎉</div>
          <div class="arrived-title">{{ arrivedTitle }}</div>
          <div class="arrived-body">{{ arrivedBody }}</div>
        </div>
      </template>
      <template v-else>
        <div class="chips">
          <div class="chip"><div class="v">{{ result.currentServingNumber ?? '—' }}</div><div class="k">คิวปัจจุบัน</div></div>
          <div class="chip">
            <div class="v">{{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}</div>
            <div class="k">คิวรอ</div>
          </div>
          <div class="chip">
            <div class="v"><template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">~{{ formattedWait }}</template><template v-else>—</template></div>
            <div class="k">เวลารอ</div>
          </div>
        </div>
      </template>

      <div class="steps">
        <div
          v-for="step in visibleSteps()"
          :key="step.key"
          class="step"
          :class="{ done: step.done, now: step.key === currentStepKey, skipped: step.skipped }"
        >
          <div class="b">
            <v-icon v-if="step.done" icon="mdi-check" size="13" color="white" />
            <v-icon v-else-if="step.skipped" icon="mdi-minus" size="13" />
            <template v-else>{{ step.key === currentStepKey ? '●' : '' }}</template>
          </div>
          <div class="l">{{ step.label }}</div>
        </div>
      </div>
      <div v-if="stageWaitText(currentStep())" class="stage-wait">{{ stageWaitText(currentStep()) }}</div>
    </div>
  </div>
</template>

<style scoped>
.tpl-restaurant {
  margin: -0.75rem;
  background: #fdf1e4;
  color: #3a2416;
  font-family: 'Mitr', sans-serif;
  border-radius: var(--r-card-lg);
  overflow: hidden;
  padding-bottom: var(--sp-4);
}

.head { background: #b5442e; color: #fff2e6; padding: var(--sp-5) var(--sp-5) 30px; border-radius: 0 0 32px 32px; text-align: center; }
.head .icon { font-size: 28px; }
.head .h { font-size: var(--fs-base); font-weight: 600; margin-top: var(--sp-1); }
.head .s { font-size: var(--fs-sm); color: #f3cdbb; margin-top: 2px; }

.card { background: #fff; margin: -20px var(--sp-4) 0; border-radius: 22px; padding: var(--sp-5); box-shadow: 0 10px 24px -12px rgba(58, 36, 22, 0.25); text-align: center; }
.num-lbl { font-size: var(--fs-sm); color: #a3866f; }
.num { font-family: 'Fredoka', sans-serif; font-weight: 700; font-size: 76px; color: #b5442e; line-height: 1.1; }
.name { font-size: var(--fs-sm); color: #6b5443; margin-top: var(--sp-1); }

.arrived { padding: var(--sp-3) 0; }
.badge { font-size: 2.2rem; animation: bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1); }
.arrived-title { color: rgb(var(--v-theme-success)); font-weight: 700; font-size: var(--fs-lg); margin-top: var(--sp-1); }
.arrived-body { font-size: var(--fs-base); color: #6b5443; }

.chips { display: flex; gap: var(--sp-2); margin: var(--sp-4) 0; }
.chip { flex: 1; background: #fdf1e4; border-radius: 14px; padding: var(--sp-3) var(--sp-1); }
.chip .v { font-family: 'Fredoka', sans-serif; font-weight: 700; font-size: var(--fs-lg); color: #1f6f6b; }
.chip .k { font-size: var(--fs-xs); color: #a3866f; margin-top: 2px; }

.steps { display: flex; justify-content: space-between; padding: 0 var(--sp-2); margin-top: var(--sp-2); }
.step { display: flex; flex-direction: column; align-items: center; gap: 5px; flex: 1; }
.step .b { width: 26px; height: 26px; border-radius: 50%; background: #f0e2d3; display: flex; align-items: center; justify-content: center; font-size: var(--fs-sm); }
.step.done .b { background: #1f6f6b; color: #fff; }
.step.now .b { background: #b5442e; color: #fff; }
.step.skipped .b { background: #e4d6c6; }
.step .l { font-size: 9.5px; color: #8a7461; text-align: center; line-height: 1.2; }
.step.now .l { color: #b5442e; font-weight: 600; }

.stage-wait { margin-top: var(--sp-3); font-size: var(--fs-sm); font-weight: 600; color: #b5442e; }

@keyframes bounce-in {
  0% { transform: scale(0.3); opacity: 0; }
  60% { transform: scale(1.15); opacity: 1; }
  100% { transform: scale(1); }
}

@media (prefers-reduced-motion: reduce) {
  .badge { animation: none !important; }
}
</style>
