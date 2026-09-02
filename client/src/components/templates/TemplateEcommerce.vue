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
  <div class="tpl-ecommerce">
    <div class="head">
      <div class="top"><span>{{ result.departmentName ?? result.groupName ?? '—' }}</span><span>{{ arrived ? 'ถึงคิวแล้ว' : 'กำลังดำเนินการ' }}</span></div>
      <div class="n">{{ result.qNumber }}</div>
      <div class="s">คิวของท่าน{{ patientLine ? ` · ${patientLine}` : '' }}</div>
    </div>

    <div class="stepper">
      <template v-for="(step, i) in visibleSteps()" :key="step.key">
        <div class="gs" :class="{ done: step.done, now: step.key === currentStepKey, skipped: step.skipped }">
          <span v-if="i > 0" class="line" :class="{ done: step.done || step.skipped }"></span>
          <div class="b">
            <v-icon v-if="step.done" icon="mdi-check" size="13" color="white" />
            <v-icon v-else-if="step.skipped" icon="mdi-minus" size="13" color="white" />
            <template v-else>{{ i + 1 }}</template>
          </div>
          <div class="l">{{ step.label }}</div>
        </div>
      </template>
    </div>

    <div class="banner" :class="{ done: arrived }">
      <template v-if="arrived">
        <div><div class="k">{{ arrivedTitle }}</div><div class="v" style="font-size:16px">{{ arrivedBody }}</div></div>
      </template>
      <template v-else>
        <div>
          <div class="k">คิวปัจจุบัน {{ result.currentServingNumber ?? '—' }}</div>
          <div class="v">{{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไปคือคุณ' : `อีก ${displayedAhead ?? result.aheadCount} คิว` }}</div>
        </div>
        <div style="text-align:right">
          <div class="k">เวลารอ</div>
          <div class="v" style="font-size:16px">
            <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">~{{ formattedWait }}</template>
            <template v-else>—</template>
          </div>
        </div>
      </template>
    </div>

    <div class="list">
      <div
        v-for="step in visibleSteps()"
        :key="step.key"
        class="li"
        :class="{ done: step.done, now: step.key === currentStepKey, skipped: step.skipped }"
      >
        <div class="box">
          <v-icon v-if="step.done" icon="mdi-check" size="11" color="white" />
          <v-icon v-else-if="step.skipped" icon="mdi-minus" size="11" />
        </div>
        <div>
          <div class="t">{{ step.label }}<template v-if="step.skipped"> (ข้าม)</template></div>
          <div v-if="step.at" class="m">{{ formatStepTime(step.at) }}</div>
          <div v-else-if="step.key === currentStepKey" class="m">
            {{ step.location ? step.location + ' · ' : '' }}{{ stageWaitText(currentStep()) || 'กำลังดำเนินการ' }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tpl-ecommerce {
  margin: -0.75rem;
  background: #fff;
  font-family: 'Kanit', 'Sarabun', sans-serif;
  border-radius: var(--r-card-lg);
  overflow: hidden;
  padding-bottom: var(--sp-4);
}

.head { background: linear-gradient(120deg, #ff6b00, #ee2b47); color: #fff; padding: var(--sp-5) var(--sp-5) 40px; border-radius: 0 0 26px 26px; }
.top { display: flex; justify-content: space-between; align-items: center; font-size: var(--fs-sm); font-weight: 500; opacity: 0.9; }
.n { font-family: 'Kanit', sans-serif; font-weight: 700; font-size: 64px; line-height: 1.05; margin-top: var(--sp-2); }
.s { font-size: var(--fs-sm); opacity: 0.9; margin-top: 2px; }

.stepper { display: flex; margin: -22px var(--sp-4) 0; background: #fff; border-radius: 18px; padding: var(--sp-4) var(--sp-2); box-shadow: 0 10px 22px -12px rgba(238, 43, 71, 0.35); }
.gs { flex: 1; display: flex; flex-direction: column; align-items: center; position: relative; }
.gs .b { width: 26px; height: 26px; border-radius: 50%; background: #f1f2f5; color: #b5b8c0; display: flex; align-items: center; justify-content: center; font-size: var(--fs-sm); }
.gs.done .b { background: #ee2b47; color: #fff; }
.gs.now .b { background: #ee2b47; color: #fff; box-shadow: 0 0 0 4px rgba(238, 43, 71, 0.18); }
.gs .line { position: absolute; top: 13px; left: -50%; width: 100%; height: 2px; background: #f1f2f5; z-index: -1; }
.gs .line.done { background: #ee2b47; }
.gs .l { font-size: 8.5px; color: #9aa0ab; margin-top: 5px; text-align: center; }
.gs.now .l { color: #ee2b47; font-weight: 600; }

.banner { margin: var(--sp-4) var(--sp-4) 0; background: #fff2ea; border-radius: 14px; padding: var(--sp-4); display: flex; align-items: center; justify-content: space-between; }
.banner.done { background: color-mix(in srgb, rgb(var(--v-theme-success)) 12%, white); }
.banner .v { font-weight: 700; font-size: 19px; color: #ee2b47; }
.banner.done .v { color: rgb(var(--v-theme-success)); }
.banner .k { font-size: var(--fs-xs); color: #b8683f; }

.list { margin: var(--sp-4) var(--sp-4) 0; display: flex; flex-direction: column; gap: var(--sp-3); }
.li { display: flex; gap: var(--sp-2); align-items: flex-start; font-size: var(--fs-sm); }
.box { width: 18px; height: 18px; border-radius: 5px; background: #f1f2f5; flex-shrink: 0; margin-top: 1px; display: flex; align-items: center; justify-content: center; }
.li.done .box { background: #ee2b47; }
.li.now .box { background: #ee2b47; box-shadow: 0 0 0 3px rgba(238, 43, 71, 0.16); }
.t { color: #333743; font-weight: 500; }
.li.skipped .t { color: #9aa0ab; font-style: italic; }
.m { color: #9aa0ab; font-size: var(--fs-xs); }
</style>
