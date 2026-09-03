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

const patientLine = computed(() => patientLineText(props.result, props.showPatientName, props.showHn));
const steps = computed(() => props.result.timeline ?? []);
const resolvedCount = computed(() => steps.value.filter((s) => s.done || s.skipped).length);
const progressPct = computed(() => (steps.value.length ? resolvedCount.value / steps.value.length : 0));
const CIRCUMFERENCE = 276.5;
const dashOffset = computed(() => CIRCUMFERENCE * (1 - progressPct.value));
const currentStep = computed(() => steps.value.find((s) => s.key === props.currentStepKey));

const liveNow = useLiveClock();
const liveTimeText = computed(() => liveNow.value.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
const visitDate = computed(() => visitDateText(props.result));
</script>

<template>
  <div class="tpl-tracking">
    <div class="topbar">
      <div class="logo">{{ (result.departmentName ?? 'รพ').slice(0, 2) }}</div>
      <div class="hospital">
        {{ result.departmentName ?? result.groupName ?? 'ระบบติดตามคิว' }}
        <small>ระบบติดตามคิวออนไลน์</small>
      </div>
    </div>

    <div v-if="patientLine" class="patient-line">{{ patientLine }}</div>
    <div class="meta-time">{{ visitDate }} เวลา {{ liveTimeText }} น.</div>

    <div class="pill" :class="{ 'pill-done': arrived }">
      <span class="pulse"></span>
      <template v-if="arrived">ถึงคิวของท่านแล้ว</template>
      <template v-else>กำลังเรียกคิว {{ result.currentServingNumber ?? '—' }}</template>
    </div>

    <div class="ring-wrap">
      <div class="ring">
        <svg viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="44" fill="none" stroke="#e7e9ef" stroke-width="9" />
          <circle
            cx="50" cy="50" r="44" fill="none" stroke="#3548e0" stroke-width="9"
            stroke-linecap="round" :stroke-dasharray="CIRCUMFERENCE" :stroke-dashoffset="dashOffset"
          />
        </svg>
        <div class="num"><span class="lbl">คิวของท่าน</span><span class="n">{{ result.qNumber }}</span></div>
      </div>
      <div v-if="!arrived && result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined" class="eta">
        ⏱ อีกประมาณ {{ formattedWait }}
      </div>
      <div v-else-if="arrived" class="eta eta-done">{{ arrivedBody }}</div>
    </div>

    <div class="timeline">
      <div
        v-for="(step, i) in steps"
        :key="step.key"
        class="step"
        :class="{ done: step.done, current: step.key === currentStepKey, skipped: step.skipped }"
        :style="i === steps.length - 1 ? 'padding-bottom:0' : ''"
      >
        <div class="rail">
          <div class="node">
            <v-icon v-if="step.done" icon="mdi-check" size="12" color="white" />
            <v-icon v-else-if="step.skipped" icon="mdi-minus" size="12" color="white" />
          </div>
          <div v-if="i < steps.length - 1" class="bar"></div>
        </div>
        <div class="content">
          <div class="t">{{ step.label }}<template v-if="step.skipped"> (ข้าม)</template></div>
          <div v-if="step.at" class="m">{{ formatStepTime(step.at) }}</div>
          <div v-else-if="step.key === currentStepKey && step.location" class="m">{{ step.location }}<template v-if="stageWaitText(step)"> · {{ stageWaitText(step) }}</template></div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-tracking {
  margin: -0.75rem;
  padding: var(--sp-5) var(--sp-4);
  background: #f6f8fc;
  font-family: 'Prompt', sans-serif;
  display: flex;
  flex-direction: column;
  gap: var(--sp-4);
  border-radius: var(--r-card-lg);
}

.topbar { display: flex; align-items: center; gap: var(--sp-2); }
.logo {
  width: 30px; height: 30px; border-radius: 9px;
  background: #eef1ff; color: #3548e0;
  display: flex; align-items: center; justify-content: center;
  font-family: 'Plus Jakarta Sans', sans-serif; font-weight: 800; font-size: 13px;
}
.hospital { font-size: var(--fs-sm); font-weight: 600; color: #1c2233; }
.hospital small { display: block; font-size: var(--fs-xs); color: #8992a6; font-weight: 400; }

.patient-line { font-size: var(--fs-sm); color: #5c6773; }
.meta-time { font-size: var(--fs-xs); color: #8992a6; font-variant-numeric: tabular-nums; }

.pill {
  align-self: flex-start;
  display: inline-flex; align-items: center; gap: 7px;
  background: #fff1e6; color: #b64e0f;
  font-size: var(--fs-sm); font-weight: 600;
  padding: var(--sp-2) var(--sp-4) var(--sp-2) var(--sp-2);
  border-radius: 999px;
}
.pill-done { background: color-mix(in srgb, rgb(var(--v-theme-success)) 14%, white); color: rgb(var(--v-theme-success)); }
.pill .pulse {
  width: 7px; height: 7px; border-radius: 50%; background: #e8720f;
  animation: pulse-opacity 1.6s ease-in-out infinite;
}
.pill-done .pulse { background: rgb(var(--v-theme-success)); }

.ring-wrap { display: flex; flex-direction: column; align-items: center; padding: var(--sp-1) 0; }
.ring { position: relative; width: 196px; height: 196px; }
.ring svg { width: 100%; height: 100%; transform: rotate(-90deg); }
.ring circle { transition: stroke-dashoffset 0.5s ease; }
.ring .num {
  position: absolute; inset: 0; display: flex; flex-direction: column;
  align-items: center; justify-content: center;
}
.ring .num .lbl { font-size: var(--fs-xs); color: #8992a6; margin-bottom: 2px; }
.ring .num .n {
  font-family: 'Plus Jakarta Sans', sans-serif; font-weight: 800; font-size: 60px;
  color: #1c2233; line-height: 1;
}
.eta {
  margin-top: var(--sp-3);
  background: #eef1ff; color: #3548e0;
  font-weight: 600; font-size: var(--fs-base);
  padding: var(--sp-2) var(--sp-5); border-radius: 999px;
}
.eta-done { background: color-mix(in srgb, rgb(var(--v-theme-success)) 14%, white); color: rgb(var(--v-theme-success)); }

.timeline { display: flex; flex-direction: column; }
.step { display: flex; gap: var(--sp-3); position: relative; padding-bottom: var(--sp-4); }
.rail { display: flex; flex-direction: column; align-items: center; }
.node {
  width: 22px; height: 22px; border-radius: 50%;
  background: #e7e9ef; display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.step.done .node { background: #3548e0; }
.step.current .node { background: #e8720f; box-shadow: 0 0 0 5px rgba(232, 114, 15, 0.16); }
.step.skipped .node { background: #c7cbd3; }
.bar { flex: 1; width: 2px; background: #e7e9ef; margin-top: 2px; min-height: 18px; }
.step.done .bar { background: #3548e0; }
.content { padding-top: 1px; }
.t { font-size: var(--fs-base); font-weight: 600; color: #1c2233; }
.step.current .t { color: #b64e0f; }
.step.skipped .t { color: #b0b5c0; font-weight: 500; }
.m { font-size: var(--fs-xs); color: #8992a6; margin-top: 1px; }

@keyframes pulse-opacity {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.35; }
}

@media (prefers-reduced-motion: reduce) {
  .pill .pulse { animation: none !important; }
}
</style>
