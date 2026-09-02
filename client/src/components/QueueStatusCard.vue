<script setup>
import { computed } from 'vue';
import { CANVAS_WIDTH, CANVAS_HEIGHT, FIELD_DEFS } from '../config/fieldLayout';
import { formatStepTime, stageWaitText } from '../utils/queueFormat';

const props = defineProps({
  result: { type: Object, required: true },
  arrived: { type: Boolean, default: false },
  currentStepKey: { type: String, default: null },
  displayedAhead: { type: Number, default: null },
  formattedWait: { type: String, default: '' },
  arrivedTitle: { type: String, default: 'ถึงคิวของท่านแล้ว' },
  arrivedBody: { type: String, default: 'กรุณาเข้ารับบริการ' },
  fieldLayout: { type: Object, required: true },
  /** Enables drag handles + a dashed outline for the admin layout editor. */
  editable: { type: Boolean, default: false },
});

const emit = defineEmits(['drag-field']);

const TIMELINE_KEYS = {
  timelineRegister: 'register',
  timelineCall: 'call',
  timelineLab: 'lab',
  timelineFin: 'fin',
  timelinePha: 'pha',
};

function stepFor(fieldId) {
  const key = TIMELINE_KEYS[fieldId];
  return props.result.timeline?.find((s) => s.key === key) ?? null;
}


// cqw = 1% of the canvas's own inline-size (needs `container-type: inline-size` on .lf-canvas) —
// so a fontSize authored against the CANVAS_WIDTH design canvas scales proportionally no matter
// how wide the canvas is actually rendered (admin's small preview vs the real ~320-480px card).
const CQW_PER_PX = 100 / CANVAS_WIDTH;

// align/vAlign pick which edge of the field's own box sits at x%/y% — see the comment on
// DEFAULT_FIELD_LAYOUT in fieldLayout.js. Timeline-group fields skip this (their icon+label row
// keeps the fixed layout it always had) — they fall back to the CSS class default below
// (translate(-50%,-50%), text-align:center on the outer box; the inner row overrides to left).
const ALIGN_TRANSLATE_X = { left: '0%', center: '-50%', right: '-100%' };
const VALIGN_TRANSLATE_Y = { top: '0%', middle: '-50%', bottom: '-100%' };

function fieldStyle(id) {
  const f = props.fieldLayout[id];
  if (!f) return {};
  const style = {
    left: `${f.x}%`,
    top: `${f.y}%`,
    fontSize: `${(f.fontSize * CQW_PER_PX).toFixed(3)}cqw`,
    color: f.color,
  };
  const isTimeline = FIELD_DEFS.find((d) => d.id === id)?.group === 'timeline';
  if (!isTimeline) {
    const align = f.align ?? 'center';
    const vAlign = f.vAlign ?? 'middle';
    style.transform = `translate(${ALIGN_TRANSLATE_X[align] ?? '-50%'}, ${VALIGN_TRANSLATE_Y[vAlign] ?? '-50%'})`;
    style.textAlign = align;
  }
  return style;
}

let dragId = null;
let canvasEl = null;

function startDrag(id, evt, canvasRef) {
  if (!props.editable) return;
  dragId = id;
  canvasEl = canvasRef;
  evt.target.setPointerCapture?.(evt.pointerId);
  evt.preventDefault();
}

function onDragMove(evt) {
  if (!dragId || !canvasEl) return;
  const rect = canvasEl.getBoundingClientRect();
  const x = Math.min(100, Math.max(0, ((evt.clientX - rect.left) / rect.width) * 100));
  const y = Math.min(100, Math.max(0, ((evt.clientY - rect.top) / rect.height) * 100));
  emit('drag-field', dragId, Math.round(x * 10) / 10, Math.round(y * 10) / 10);
}

function endDrag() {
  dragId = null;
  canvasEl = null;
}

// The reference design does NOT draw a line stretching between consecutive step icons (that was
// wrong — checked closely against a cropped/upscaled `1788160383287.jpg`: a "done" step that has
// location/time meta lines underneath gets only a short stub hanging directly below its own icon,
// not a line reaching down to the next icon; pending steps with no meta text just happen to sit
// close enough together that their own short stubs read as one continuous line). So each step
// renders its own fixed-length stub, colored by whether THAT step is done — no cross-step geometry
// needed at all. Only the last visible timeline step has no stub (nothing below it to point to).
const TIMELINE_ORDER = ['timelineRegister', 'timelineCall', 'timelineLab', 'timelineFin', 'timelinePha'];

const lastVisibleTimelineId = computed(() => {
  const visible = TIMELINE_ORDER.filter((id) => props.fieldLayout[id]?.visible);
  return visible[visible.length - 1] ?? null;
});
</script>

<template>
  <div
    class="lf-canvas"
    :style="{ aspectRatio: `${CANVAS_WIDTH} / ${CANVAS_HEIGHT}` }"
    @pointermove="onDragMove"
    @pointerup="endDrag"
    @pointercancel="endDrag"
  >
    <template v-for="def in FIELD_DEFS" :key="def.id">
      <div
        v-if="fieldLayout[def.id]?.visible && !(def.group === 'stats' && arrived) && !(def.id === 'hn' && fieldLayout.patientName?.visible)"
        class="lf-field"
        :class="{ 'lf-editable': editable }"
        :style="fieldStyle(def.id)"
        @pointerdown="(e) => startDrag(def.id, e, e.currentTarget.closest('.lf-canvas'))"
      >
        <!-- header fields — patientName absorbs HN onto the same line (with a "•" separator)
             whenever both are visible, matching the reference design; HN only gets its own
             standalone position/line if patientName has been hidden. -->
        <div v-if="def.id === 'queueLabel'" class="lf-text">หมายเลขคิวของท่าน</div>
        <div v-else-if="def.id === 'queueNumber'" class="lf-queue-number">{{ result.qNumber }}</div>
        <div v-else-if="def.id === 'patientName'" class="lf-text">
          {{ result.patientName }}<template v-if="result.patientName && fieldLayout.hn?.visible && result.hn"> • HN: {{ result.hn }}</template>
        </div>
        <div v-else-if="def.id === 'hn'" class="lf-text">HN: {{ result.hn }}</div>

        <!-- stats + wait -->
        <div v-else-if="def.id === 'statCurrent'" class="lf-stat-box">
          <div class="lf-stat-label">เลขคิวปัจจุบัน</div>
          <div class="lf-stat-value">{{ result.currentServingNumber ?? '—' }}</div>
        </div>
        <div v-else-if="def.id === 'statAhead'" class="lf-stat-box">
          <div class="lf-stat-label">จำนวนคิวรอ</div>
          <div class="lf-stat-value">
            {{ (result.aheadCount ?? 0) <= 0 ? 'ถัดไป' : `อีก ${displayedAhead ?? result.aheadCount} คิว` }}
          </div>
        </div>
        <div v-else-if="def.id === 'waitTime'" class="lf-wait-box">
          <div class="lf-stat-label">ระยะเวลารอโดยประมาณ</div>
          <template v-if="result.estimatedWaitMinutes !== null && result.estimatedWaitMinutes !== undefined">
            <div class="lf-wait-value">อีกประมาณ {{ formattedWait }}</div>
          </template>
          <div v-else class="lf-text">ยังไม่มีข้อมูลเพียงพอสำหรับประมาณเวลาวันนี้</div>
        </div>

        <!-- timeline steps -->
        <div v-else-if="def.group === 'timeline'" class="lf-timeline-step">
          <span class="lf-step-icon">
            <v-icon v-if="stepFor(def.id)?.done" icon="mdi-check-circle" color="primary" size="20" />
            <v-icon
              v-else-if="stepFor(def.id)?.skipped"
              icon="mdi-minus-circle-outline"
              color="grey-lighten-1"
              size="20"
            />
            <v-icon
              v-else-if="stepFor(def.id)?.key === currentStepKey"
              icon="mdi-circle-slice-8"
              size="20"
              class="lf-step-icon-current"
            />
            <v-icon v-else icon="mdi-checkbox-blank-circle-outline" color="grey" size="20" />
            <span
              v-if="def.id !== lastVisibleTimelineId"
              class="lf-step-stub"
              :class="stepFor(def.id)?.done ? 'lf-stub-done' : 'lf-stub-pending'"
            />
          </span>
          <span class="lf-step-content">
            <span class="lf-step-label" :class="{ 'lf-step-skipped': stepFor(def.id)?.skipped }">
              {{ def.label }}<template v-if="stepFor(def.id)?.skipped"> (ข้าม)</template>
            </span>
            <span v-if="stepFor(def.id)?.location" class="lf-step-meta">จุดบริการ: {{ stepFor(def.id).location }}</span>
            <span v-if="stepFor(def.id)?.at" class="lf-step-meta">{{ formatStepTime(stepFor(def.id).at) }}</span>
            <span v-if="stageWaitText(stepFor(def.id))" class="lf-step-meta lf-step-wait">{{ stageWaitText(stepFor(def.id)) }}</span>
          </span>
        </div>

        <!-- footer -->
        <div v-else-if="def.id === 'footer'" class="lf-footer">
          <div class="lf-text">
            จุดบริการ: {{ result.departmentName ?? result.groupName ?? '—' }}
            <template v-if="result.visitNumber"> • เลขที่รับบริการ: {{ result.visitNumber }}</template>
          </div>
          <div class="lf-footer-strong">หมายเลขคิวของคุณ: {{ result.qNumber }}</div>
        </div>
      </div>
    </template>

    <!-- Arrived celebration — fixed slot, not part of the customizable field system. -->
    <div v-if="arrived" class="lf-field lf-arrived">
      <div class="lf-arrived-badge">🎉</div>
      <div class="lf-arrived-title">{{ arrivedTitle }}</div>
      <div class="lf-text">{{ arrivedBody }}</div>
    </div>
  </div>
</template>

<style scoped>
.lf-canvas {
  position: relative;
  width: 100%;
  container-type: inline-size;
}

.lf-field {
  position: absolute;
  transform: translate(-50%, -50%);
  /* `width: max-content` sidesteps a shrink-to-fit quirk: with `left: X%` set, a browser computes
     an absolutely-positioned element's auto width using only the space from that left offset to
     the containing block's edge — BEFORE the translate(-50%) recenters it — so text was wrapping
     as if it only had half the canvas to work with. max-content plus an explicit max-width fixes
     that without losing real wrapping once content actually is too wide. */
  width: max-content;
  max-width: 90cqw;
  white-space: nowrap;
  text-align: center;
}

.lf-editable {
  cursor: grab;
  outline: 1px dashed color-mix(in srgb, currentColor 35%, transparent);
  outline-offset: 4px;
  border-radius: 4px;
  touch-action: none;
}

.lf-editable:active {
  cursor: grabbing;
}

/* The queue number is a deliberate "hero" exception to the 400/500-only weight rule and the
   5-size type scale — it plays the same oversized, high-contrast role a bank app's balance figure
   plays (see the reference design), and needs to stay legible from across a waiting room. */
.lf-queue-number {
  font-weight: 800;
  line-height: 1;
  letter-spacing: 0.02em;
}

.lf-text {
  line-height: 1.3;
  white-space: normal;
}

.lf-stat-box,
.lf-wait-box {
  background: rgba(0, 0, 0, 0.04);
  border-radius: 3.5cqw;
  padding: 1.5cqw 2.5cqw;
  min-width: 20cqw;
}

.lf-wait-box {
  max-width: 65cqw;
  white-space: normal;
}

.lf-stat-label,
.lf-step-meta {
  font-size: 0.65em;
  opacity: 0.65;
  display: block;
}

.lf-step-wait {
  color: var(--accent-color, #e8720f);
  opacity: 1;
  font-weight: 500;
}

.lf-stat-value,
.lf-wait-value {
  font-weight: 500;
}

.lf-timeline-step {
  display: inline-flex;
  align-items: flex-start;
  gap: 0.5em;
  white-space: normal;
  text-align: left;
}

.lf-step-content {
  display: flex;
  flex-direction: column;
  padding-top: 0.1em;
}

.lf-step-skipped {
  color: #9298a3;
  font-style: italic;
}

.lf-step-icon {
  display: inline-flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
}

/* Short fixed-length stub hanging below a step's own icon (see the note on lastVisibleTimelineId
   in the script) — not a line reaching down to the next step. */
.lf-step-stub {
  width: 2px;
  height: 0.9em;
  margin-top: 0.15em;
  border-radius: 1px;
}

.lf-stub-done {
  background: rgb(var(--v-theme-primary));
}

.lf-stub-pending {
  background: #ccd2db;
}

.lf-step-icon-current {
  color: var(--accent-color, #e8720f) !important;
}

.lf-footer-strong {
  font-weight: 500;
}

.lf-arrived {
  /* Anchored lower than the true vertical center of the vacated stats/wait area, and shrunk down
     (see below) — at 32%/9cqw this block was tall enough to reach up into the header (patient
     name / HN) fields on narrow real-mobile widths, even though it looked fine on a wide desktop
     screenshot. */
  top: 38%;
  left: 50%;
  width: 82%;
}

.lf-arrived-badge {
  font-size: 6.5cqw;
  line-height: 1;
  margin-bottom: 0.1em;
  animation: lf-bounce-in 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
}

@keyframes lf-bounce-in {
  0% { transform: scale(0.3); opacity: 0; }
  60% { transform: scale(1.15); opacity: 1; }
  100% { transform: scale(1); }
}

.lf-arrived-title {
  color: rgb(var(--v-theme-success));
  font-weight: 500;
  font-size: 5cqw;
  margin-bottom: 0.15em;
}

@media (prefers-reduced-motion: reduce) {
  .lf-arrived-badge {
    animation: none !important;
  }
}
</style>
