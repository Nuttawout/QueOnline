// Font choices offered in the admin panel. All weights are imported in main.js via @fontsource
// (self-hosted, no external font CDN). Add a new option here + import its weights in main.js.
export const FONT_OPTIONS = [
  { value: 'Anuphan', label: 'Anuphan (แนะนำ — โมเดิร์นสุด)' },
  { value: 'IBM Plex Sans Thai', label: 'IBM Plex Sans Thai' },
  { value: 'Sarabun', label: 'Sarabun' },
  { value: 'Kanit', label: 'Kanit' },
  { value: 'Noto Sans Thai', label: 'Noto Sans Thai' },
];

export const FONT_SIZE_OPTIONS = [
  { value: 'small', label: 'เล็ก', scale: 0.88 },
  { value: 'medium', label: 'กลาง (ค่าเริ่มต้น)', scale: 1 },
  { value: 'large', label: 'ใหญ่', scale: 1.18 },
];

export function fontSizeScaleValue(key) {
  return FONT_SIZE_OPTIONS.find((o) => o.value === key)?.scale ?? 1;
}
