/** Shared by QueueStatusCard.vue and every fixed card template — formats a timeline step's ISO
 * timestamp the same way everywhere ("14:01", Thai locale, 24h). */
export function formatStepTime(iso) {
  if (!iso) return '';
  return new Date(iso).toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' });
}

/** "X ชม. Y นาที" — shared by the top-level pre-call wait estimate (QueueStatusView.vue) and each
 * template's per-stage estimate (lab/fin/pha AheadCount/EstimatedWaitMinutes on a QueueTimelineStep,
 * see the doc comment on that model) so both read the same way. */
export function formatWaitMinutes(minutes) {
  if (minutes === null || minutes === undefined) return '';
  if (minutes < 1) return 'อีกไม่ถึง 1 นาที';
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  if (hours <= 0) return `${mins} นาที`;
  return mins > 0 ? `${hours} ชม. ${mins} นาที` : `${hours} ชม.`;
}

/** "อีก N คิว" (+ wait estimate if we have one) for a timeline step carrying AheadCount/
 * EstimatedWaitMinutes — every template's single formatting rule for the new per-stage estimate. */
export function stageWaitText(step) {
  if (!step || step.aheadCount === null || step.aheadCount === undefined) return '';
  if (step.aheadCount <= 0) return 'ถัดไปคือคุณ';
  const wait = step.estimatedWaitMinutes;
  const waitPart = wait === null || wait === undefined ? '' : ` (~${formatWaitMinutes(wait)})`;
  return `อีก ${step.aheadCount} คิว${waitPart}`;
}

const THAI_WEEKDAYS = ['อาทิตย์', 'จันทร์', 'อังคาร', 'พุธ', 'พฤหัสบดี', 'ศุกร์', 'เสาร์'];
const THAI_MONTHS = [
  'มกราคม', 'กุมภาพันธ์', 'มีนาคม', 'เมษายน', 'พฤษภาคม', 'มิถุนายน',
  'กรกฎาคม', 'สิงหาคม', 'กันยายน', 'ตุลาคม', 'พฤศจิกายน', 'ธันวาคม',
];

/** "วันพุธที่ 3 เดือน กันยายน พ.ศ. 2569" — full Thai date with weekday + Buddhist-era year, spelled
 * out by hand (not Intl.DateTimeFormat) so the wording matches exactly what was asked for instead
 * of whatever shorthand th-TH locale formatting happens to produce. */
export function formatVisitDate(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  const weekday = THAI_WEEKDAYS[d.getDay()];
  const month = THAI_MONTHS[d.getMonth()];
  const buddhistYear = d.getFullYear() + 543;
  return `วัน${weekday}ที่ ${d.getDate()} เดือน ${month} พ.ศ. ${buddhistYear}`;
}

/** The visit date shown on every template ("วันที่มารับบริการ") — taken from the timeline's
 * "register" step, which is always populated (see BuildTimeline server-side: qdatetime is the
 * queue row's creation timestamp, never null) rather than a separate field re-fetched per template. */
export function visitDateText(result) {
  const at = result?.timeline?.find((s) => s.key === 'register')?.at;
  return formatVisitDate(at);
}

/** "ชื่อ · HN 123456" (or just one half, or '') — every one of the 10 fixed templates' single rule
 * for whether/how to show the patient's name and HN, driven by the admin-configurable
 * settings.showPatientName/showHn toggles (2026-09-02: added after a real gap — several templates
 * were silently never showing name/HN at all). Not used by the "custom" free-form layout, which
 * already has its own finer-grained per-field visibility toggle (fieldLayout.js). */
export function patientLineText(result, showPatientName, showHn) {
  const parts = [];
  if (showPatientName && result?.patientName) parts.push(result.patientName);
  if (showHn && result?.hn) parts.push(`HN ${result.hn}`);
  return parts.join(' · ');
}
