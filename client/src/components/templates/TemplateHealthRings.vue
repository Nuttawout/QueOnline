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
const steps = computed(() => props.result.timeline ?? []);
const resolvedCount = computed(() => steps.value.filter((s) => s.done || s.skipped).length);
const outerFrac = computed(() => (steps.value.length ? resolvedCount.value / steps.value.length : 0));
const innerFrac = computed(() => {
  const ahead = props.result.aheadCount ?? 3;
  return Math.max(0.06, Math.min(1, 1 - ahead / 5));
});
const OUTER_C = 276.5;
const INNER_C = 207.3;
</script>

<template>
  <div class="tpl-rings">
    <div class="brand">{{ result.departmentName ?? result.groupName ?? '—' }}</div>
    <div v-if="patientLine" class="patient-line">{{ patientLine }}</div>
    <div class="rings">
      <svg viewBox="0 0 100 100">
        <circle cx="50" cy="50" r="44" fill="none" stroke="#1c1d21" stroke-width="7" />
        <circle
          cx="50" cy="50" r="44" fill="none" stroke="#3fa8ff" stroke-width="7" stroke-linecap="round"
          :stroke-dasharray="OUTER_C" :stroke-dashoffset="OUTER_C * (1 - outerFrac)"
        />
        <circle cx="50" cy="50" r="33" fill="none" stroke="#1c1d21" stroke-width="7" />
        <circle
          cx="50" cy="50" r="33" fill="none" stroke="#ff8a3d" stroke-width="7" stroke-linecap="round"
          :stroke-dasharray="INNER_C" :stroke-dashoffset="INNER_C * (1 - innerFrac)"
        />
      </svg>
      <div class="mid">
        <span class="n">{{ result.qNumber }}</span>
        <span class="l">คิวของท่าน</span>
      </div>
    </div>

    <div class="stats">
      <div class="stat">
        <span class="dot" style="background:#3fa8ff"></span>
        <div class="v">{{ arrived ? 'ถึงแล้ว' : (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount}` }}</div>
        <div class="k">คิวรอ</div>
      </div>
      <div class="stat">
        <span class="dot" style="background:#ff8a3d"></span>
        <div class="v">
          <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">~{{ formattedWait }}</template>
          <template v-else>—</template>
        </div>
        <div class="k">เวลารอ</div>
      </div>
      <div class="stat">
        <span class="dot" style="background:#37c978"></span>
        <div class="v">{{ resolvedCount }}/{{ steps.length }}</div>
        <div class="k">ขั้นตอน</div>
      </div>
    </div>

    <div class="timeline">
      <div
        v-for="step in steps"
        :key="step.key"
        class="t"
        :class="{ on: step.done, now: step.key === currentStepKey, skipped: step.skipped }"
      >
        <span class="c"></span>
        {{ step.label }}<template v-if="step.skipped"> (ข้าม)</template><template v-if="step.at"> · {{ formatStepTime(step.at) }}</template><template v-else-if="step.key === currentStepKey && step.location"> · {{ step.location }}</template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-rings {
  margin: -0.75rem;
  background: #000;
  color: #f2f3f5;
  font-family: 'Manrope', 'Noto Sans Thai', sans-serif;
  padding: var(--sp-5) var(--sp-4);
  border-radius: var(--r-card-lg);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--sp-4);
}

.brand { font-size: var(--fs-sm); color: #7c7f87; font-weight: 700; letter-spacing: 0.06em; }
.patient-line { font-size: var(--fs-sm); color: #b8bcc4; margin-top: -8px; }

.rings { position: relative; width: 208px; height: 208px; margin: 4px 0; }
.rings svg { width: 100%; height: 100%; transform: rotate(-90deg); }
.rings circle { transition: stroke-dashoffset 0.5s ease; }
.mid { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; }
.mid .n { font-family: 'Manrope', sans-serif; font-weight: 800; font-size: 52px; line-height: 1; }
.mid .l { font-size: var(--fs-xs); color: #8b8e96; margin-top: 4px; }

.stats { display: flex; gap: var(--sp-2); width: 100%; }
.stat { flex: 1; background: #17181c; border-radius: 16px; padding: var(--sp-3) var(--sp-2); text-align: center; }
.stat .dot { display: block; width: 8px; height: 8px; border-radius: 50%; margin: 0 auto 6px; }
.stat .v { font-family: 'Manrope', sans-serif; font-weight: 800; font-size: var(--fs-lg); }
.stat .k { font-size: var(--fs-xs); color: #8b8e96; margin-top: 2px; }

.timeline { width: 100%; display: flex; flex-direction: column; gap: var(--sp-2); margin-top: 2px; }
.t { display: flex; align-items: center; gap: 9px; font-size: var(--fs-sm); color: #6d7078; }
.t .c { width: 7px; height: 7px; border-radius: 50%; background: #3a3c42; flex-shrink: 0; }
.t.on { color: #e6e7ea; }
.t.on .c { background: #3fa8ff; }
.t.now { color: #ff8a3d; font-weight: 700; }
.t.now .c { background: #ff8a3d; }
.t.skipped { color: #4a4d55; font-style: italic; }
.t.skipped .c { background: #3a3c42; border: 1px dashed #5a5d65; }
</style>
