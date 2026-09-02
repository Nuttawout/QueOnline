// Free-form layout system for the queue status card. Every field the hospital can reposition is
// listed here with an id, a human label (for the admin editor), sample text (for the admin's
// live preview, since real queue data isn't available there), and a default
// {x,y,fontSize,color,align,vAlign}. `align`/`vAlign` pick which edge of the field's own box sits
// at x/y — 'left'/'top' anchors that edge at x/y (so x/y literally reads as "distance from that
// edge"), 'right'/'bottom' anchors the opposite edge, and the default 'center'/'middle' keeps the
// original behavior of centering the box on x/y regardless of its own width/height. A field at
// x=50,align=center is horizontally centered regardless of its own width. Defaults are tuned to
// roughly reproduce the original hand-built layout; the whole point of this system is that an
// admin can drag things from there to wherever they actually want. (Timeline-group fields ignore
// align/vAlign — their icon+label row has its own fixed internal layout, see QueueStatusCard.vue.)
export const CANVAS_WIDTH = 320;
export const CANVAS_HEIGHT = 520;

export const FIELD_GROUPS = [
  { id: 'header', label: 'เลขคิว + ชื่อ + HN' },
  { id: 'stats', label: 'สถิติ + เวลารอ' },
  { id: 'timeline', label: 'ไทม์ไลน์ 5 สเตจ' },
  { id: 'footer', label: 'Footer' },
];

export const FIELD_DEFS = [
  { id: 'queueLabel', group: 'header', label: 'ป้ายกำกับ "หมายเลขคิวของท่าน"', sample: 'หมายเลขคิวของท่าน' },
  { id: 'queueNumber', group: 'header', label: 'หมายเลขคิว', sample: 'A5' },
  { id: 'patientName', group: 'header', label: 'ชื่อคนไข้', sample: 'นาย ตัวอย่าง ใจดี' },
  { id: 'hn', group: 'header', label: 'HN', sample: 'HN: 123456' },
  { id: 'statCurrent', group: 'stats', label: 'เลขคิวปัจจุบัน', sample: 'A3' },
  { id: 'statAhead', group: 'stats', label: 'จำนวนคิวรอ', sample: 'อีก 2 คิว' },
  { id: 'waitTime', group: 'stats', label: 'เวลารอโดยประมาณ', sample: 'อีกประมาณ 12 นาที' },
  { id: 'timelineRegister', group: 'timeline', label: 'ลงทะเบียน', sample: 'ลงทะเบียน · 13:51' },
  { id: 'timelineCall', group: 'timeline', label: 'เรียกเข้าห้องตรวจ', sample: 'เรียกเข้าห้องตรวจ · 14:01' },
  { id: 'timelineLab', group: 'timeline', label: 'ตรวจแล็ป', sample: 'ตรวจแล็ป' },
  { id: 'timelineFin', group: 'timeline', label: 'การเงิน', sample: 'การเงิน' },
  { id: 'timelinePha', group: 'timeline', label: 'ห้องยา', sample: 'ห้องยา' },
  { id: 'footer', group: 'footer', label: 'จุดบริการ / เลขที่รับบริการ', sample: 'จุดบริการ: อายุรกรรม • เลขที่รับบริการ: 1' },
];

export const ALIGN_OPTIONS = [
  { value: 'left', label: 'ซ้าย', icon: 'mdi-format-horizontal-align-left' },
  { value: 'center', label: 'กลาง', icon: 'mdi-format-horizontal-align-center' },
  { value: 'right', label: 'ขวา', icon: 'mdi-format-horizontal-align-right' },
];

export const VALIGN_OPTIONS = [
  { value: 'top', label: 'บน', icon: 'mdi-format-vertical-align-top' },
  { value: 'middle', label: 'กึ่งกลาง', icon: 'mdi-format-vertical-align-center' },
  { value: 'bottom', label: 'ล่าง', icon: 'mdi-format-vertical-align-bottom' },
];

export const DEFAULT_FIELD_LAYOUT = {
  queueLabel: { visible: true, x: 50, y: 2.5, fontSize: 12, color: '#5c6773', align: 'center', vAlign: 'middle' },
  queueNumber: { visible: true, x: 50, y: 8, fontSize: 44, color: '#e8720f', align: 'center', vAlign: 'middle' },
  patientName: { visible: true, x: 50, y: 16, fontSize: 14, color: '#5c6773', align: 'center', vAlign: 'middle' },
  hn: { visible: true, x: 50, y: 20, fontSize: 13, color: '#5c6773', align: 'center', vAlign: 'middle' },
  statCurrent: { visible: true, x: 26, y: 27, fontSize: 20, color: '#102a4d', align: 'center', vAlign: 'middle' },
  statAhead: { visible: true, x: 74, y: 27, fontSize: 20, color: '#102a4d', align: 'center', vAlign: 'middle' },
  waitTime: { visible: true, x: 50, y: 42, fontSize: 15, color: '#102a4d', align: 'center', vAlign: 'middle' },
  timelineRegister: { visible: true, x: 33, y: 58, fontSize: 13, color: '#1a2340', align: 'left', vAlign: 'middle' },
  timelineCall: { visible: true, x: 33, y: 65, fontSize: 13, color: '#1a2340', align: 'left', vAlign: 'middle' },
  timelineLab: { visible: true, x: 33, y: 72, fontSize: 13, color: '#1a2340', align: 'left', vAlign: 'middle' },
  timelineFin: { visible: true, x: 33, y: 79, fontSize: 13, color: '#1a2340', align: 'left', vAlign: 'middle' },
  timelinePha: { visible: true, x: 33, y: 86, fontSize: 13, color: '#1a2340', align: 'left', vAlign: 'middle' },
  // Left-aligned by default (not 'center' like everything else) — matches the reference design's
  // footer, which has always read left-to-right rather than centered.
  footer: { visible: true, x: 50, y: 96, fontSize: 12, color: '#5c6773', align: 'left', vAlign: 'middle' },
};

/** Merge saved layout (which may be missing newer fields, or a raw JSON string) onto the defaults. */
export function normalizeFieldLayout(raw) {
  let parsed = raw;
  if (typeof raw === 'string') {
    try {
      parsed = JSON.parse(raw);
    } catch {
      parsed = null;
    }
  }
  const merged = {};
  for (const def of FIELD_DEFS) {
    merged[def.id] = { ...DEFAULT_FIELD_LAYOUT[def.id], ...(parsed?.[def.id] ?? {}) };
  }
  return merged;
}
