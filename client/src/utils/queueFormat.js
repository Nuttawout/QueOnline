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
