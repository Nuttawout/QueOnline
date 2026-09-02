// Registry of the fixed-design queue-status card presets — each is a real, structurally distinct
// layout (not just a recolor), picked as a whole via `settings.cardTemplate`. "custom" is NOT one
// of these; it's the pre-existing free-form drag-and-drop system (QueueStatusCard.vue +
// fieldLayout.js) and stays a separate top-level choice in the admin picker.
//
// 2026-09-02: this is the SECOND full set of 10 — replaced the original set entirely per the
// user's explicit request ("เอาทั้ง 10 นี้มาแทนของเดิมทั้งหมดเลยครับ ผมชอบทุกอันเลย") after seeing a
// gallery of 10 real-world-referenced directions and liking all of them. Each id here is new and
// does not match any of the old set's ids — see log/2026-09-02.md for the full list of what a
// hospital that already had one of the old ids saved will fall back to (the "custom" free-form
// layout, per QueueStatusView.vue's existing unmatched-id fallback — not a crash).
export const CARD_TEMPLATES = [
  { id: 'boardingPass', label: 'บัตรผ่านขึ้นเครื่อง', description: 'แถบกรมท่าเข้ม รอยปรุ บาร์โค้ด — ทางการ น่าเชื่อถือ แบบตั๋วเครื่องบิน' },
  { id: 'tracking', label: 'ติดตามสถานะ', description: 'วงแหวนความคืบหน้า ป้ายไลฟ์กะพริบ ไทม์ไลน์แนวตั้ง — อบอุ่นแบบแอปเดลิเวอรี' },
  { id: 'board', label: 'จอบอร์ดแสดงคิว', description: 'พื้นเข้ม ตัวเลขยักษ์ อ่านชัดจากระยะไกล — แบบจอคิวธนาคาร/ป้ายเที่ยวบิน' },
  { id: 'healthRings', label: 'นาฬิกาสุขภาพ', description: 'พื้นดำ วงแหวนหลายชั้น กระชับที่สุด — แบบหน้าปัด Apple Watch' },
  { id: 'restaurant', label: 'คิวร้านอาหาร', description: 'โทนอุ่น การ์ดลอยเหนือแถบสี ตัวเลขกลมมน — แบบแอปจองร้านอาหาร' },
  { id: 'official', label: 'เอกสารราชการ', description: 'กระดาษ ตราวงกลม ตารางแบบหนังสือราชการ — เป็นทางการที่สุด' },
  { id: 'ecommerce', label: 'ติดตามพัสดุ', description: 'ไล่เฉดส้ม-แดง สเต็ปเปอร์ไอคอนกลม — คุ้นตาแบบแอปช้อปปิ้ง' },
  { id: 'swiss', label: 'มินิมอลสวิส', description: 'ขาวดำ เส้นบาง ตัวเลขจัดคอลัมน์ — พรีเมียม สงบ ไม่รกตา' },
  { id: 'neumorphic', label: 'ปุ่มนูนนีโอมอร์ฟิก', description: 'โทนเดียวทั้งจอ เงานูน-เงาบุ๋มแทนเส้นขอบ — นุ่มนวลจับต้องได้' },
  { id: 'cinema', label: 'ตั๋วหนัง', description: 'ม่วง-ชมพูนีออน ขอบฉีกซิกแซก — สนุกมีชีวิตชีวาที่สุด' },
];

// NOT a preset id — matches the server default (see AppSettingsModels.cs CardTemplate doc): the
// free-form drag-and-drop system was the only thing that existed before this feature, so it has
// to stay the fallback everywhere (before settings load, or if the fetch fails) or an already-
// customized hospital's layout would appear to silently change.
export const DEFAULT_CARD_TEMPLATE = 'custom';
