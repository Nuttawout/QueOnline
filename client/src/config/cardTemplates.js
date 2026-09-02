// Registry of the fixed-design queue-status card presets — each is a real, structurally distinct
// layout (not just a recolor), picked as a whole via `settings.cardTemplate`. "custom" is NOT one
// of these; it's the pre-existing free-form drag-and-drop system (QueueStatusCard.vue +
// fieldLayout.js) and stays a separate top-level choice in the admin picker.
export const CARD_TEMPLATES = [
  { id: 'classic', label: 'ทางการ', description: 'ไทม์ไลน์แนวตั้ง การ์ดเดียวจบ — อ่านง่าย เป็นทางการ' },
  { id: 'ticket', label: 'ตั๋วคิว', description: 'สไตล์ตั๋วโดยสาร ฉีกครึ่ง เลขคิวเด่นด้านซ้าย' },
  { id: 'hero', label: 'แบนเนอร์เด่น', description: 'แถบสีพื้นหลังเต็ม เลขคิวใหญ่ที่สุด เห็นชัดจากระยะไกล' },
  { id: 'dashboard', label: 'แดชบอร์ด', description: 'กระชับ แบบ tile สรุปข้อมูล เห็นครบในแวบเดียว' },
  { id: 'timelineFocus', label: 'เน้นไทม์ไลน์', description: 'พื้นที่ส่วนใหญ่ของการ์ดเน้นที่ขั้นตอน เห็นว่าอยู่จุดไหนชัดเจน' },
  { id: 'gradient', label: 'ไล่เฉดสี', description: 'พื้นหลังไล่เฉดสีธีมทั้งการ์ด แผงข้อมูลสีขาวลอยตรงกลาง สดใสมองเห็นง่าย' },
  { id: 'duoSplit', label: 'แบ่งครึ่งสี', description: 'แถบสีทึบด้านซ้ายเต็มความสูง คู่กับเนื้อหาสีขาวด้านขวา ตัดกันชัดเจน' },
  { id: 'glow', label: 'กรอบเรืองแสง', description: 'การ์ดเรียบสีขาว ล้อมกรอบด้วยเส้นไล่เฉดสีธีม + แสงเรืองรอบการ์ด' },
  { id: 'bubble', label: 'นุ่มนวล', description: 'โทนสีพาสเทลอ่อนละมุน ทุกอย่างโค้งมนเป็นแคปซูล เป็นมิตร' },
  { id: 'dark', label: 'ธีมมืด', description: 'พื้นหลังเข้ม ตัวอักษร/สีเน้นตัดกันสว่างชัด เด่นในที่แสงจ้าหรือกลางคืน' },
];

// NOT a preset id — matches the server default (see AppSettingsModels.cs CardTemplate doc): the
// free-form drag-and-drop system was the only thing that existed before this feature, so it has
// to stay the fallback everywhere (before settings load, or if the fetch fails) or an already-
// customized hospital's layout would appear to silently change.
export const DEFAULT_CARD_TEMPLATE = 'custom';
