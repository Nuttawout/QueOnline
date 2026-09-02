# คำแนะนำโปรเจกต์ 

## กฎ changelog (บังคับ / permanent)

**ทุกครั้งที่ปรับอะไร** (proc/schema/collation/config ที่มีผลต่อ DB หรือพฤติกรรมแอป) ให้บันทึก log **แยกเป็นไฟล์รายวัน** ที่ **`log/YYYY-MM-DD.md`**

- 1 หัวข้อต่อ 1 รอบการแก้: **แก้อะไร/ทำไม · ไฟล์ที่แตะ · ขั้นตอน deploy · สิ่งที่ค้าง (ถ้ามี)** เรียงใหม่สุดไว้บนในไฟล์ของวันนั้น
- ถ้ายังไม่มีไฟล์ของวันนี้ให้สร้างใหม่ (ใช้วันที่จริง)
- อัปเดตสารบัญ **`CHANGELOG.md`** (ราก repo): เพิ่มบรรทัดชี้ไฟล์วันนั้น (ใหม่สุดบน) + ดูแลรายการ **ค้าง/TODO** ข้ามวัน
- เป้าหมาย: **session/คนทำงานวันหลังเปิด `CHANGELOG.md` เห็นว่ามีวันไหนบ้าง แล้วเจาะเข้า `log/<วัน>.md` ทำต่อได้ทันที ไม่ต้องไล่อ่านโค้ด/commit ทั้งหมด**

## กฎคู่มือการใช้งาน (บังคับ / permanent, ตั้งแต่ 2026-08-17)

**ทุกครั้งที่แก้หรือปรับ UI/behavior ที่ผู้ใช้เห็น/ใช้งานได้จริง** (ปุ่มใหม่/ย้ายปุ่ม, filter ใหม่, field เปลี่ยน, flow เปลี่ยน ฯลฯ) ให้เอาไปอัปเดตใน **`client/public/manual/claim-guide.html`** (คู่มือการใช้งาน AIAPRO ลิงก์จากหน้า Login) ให้ตรงกับของจริงด้วยเสมอ — ห้ามปล่อยให้คู่มือตกยุคจนต้องมานั่งไล่ทำใหม่ทีเดียวทั้งฉบับ

- อัปเดตทั้งข้อความอธิบายและ screenshot ประกอบ (ถ้าจุดที่แก้มีผลต่อหน้าตาที่เห็น)
- ถ่าย screenshot ใหม่จาก dev environment เท่านั้น (mint JWT เองจาก `server/.env`'s `JWT_SECRET` ไม่ล็อกอิน/ขอ credential จริงจาก production) — โค้ดชุดเดียวกับที่ deploy จริงหน้าตาเหมือนกันทุกประการ ข้อมูลทดสอบใน dev DB mask ด้วย x ที่ต้นทางอยู่แล้วปลอดภัยตาม PDPA
- ฟีเจอร์ที่ยังใช้งานจริงไม่ได้ (เช่น รอ config เพิ่ม) ให้ใส่หมายเหตุเตือนในคู่มือด้วย (ใช้ CSS class `.note` สีส้ม ที่มีอยู่แล้ว) ไม่ใช่แค่เงียบไว้
- เป้าหมาย: **ไม่ต้องเสียเวลากลับมาทำคู่มือใหม่ทีหลังทีเดียวยกฉบับ** — อัปเดตสดตามรอบที่แก้โค้ดไปเลย

## กฎ UI / ดีไซน์ (บังคับ / permanent)

ระบบดีไซน์ของ frontend คือธีม **"Klein Pro"** — แหล่งความจริงเดียวอยู่ที่ **`client/src/theme.js`** (โทเคน JS + ธีม ant-design-vue) และ **`client/src/styles/tokens.css`** (CSS var `--mc-*`) สองไฟล์นี้ต้องมีค่าตรงกันเสมอ

- **ห้าม hardcode ค่าสี/ฟอนต์/มุมโค้ง** ในไฟล์ `.vue` — ให้ใช้ CSS var: `var(--mc-primary)`, `var(--mc-accent)`, `var(--mc-ink)`, `var(--mc-muted)`, `var(--mc-line)`, `var(--mc-canvas)`, `var(--mc-card)`, `var(--mc-header)`, `var(--mc-radius)` ฯลฯ (สี semantic: `--mc-success/--mc-warning/--mc-info/--mc-error` + คู่ `*-bg`)
- คอมโพเนนต์ ant-design-vue ปรับสีผ่าน `<a-config-provider :theme>` ที่ `App.vue` (ดึงจาก `theme.js`) — **ไม่ override สีทับ component ตรงๆ**
- **ฟอนต์แบรนด์ = IBM Plex Sans Thai** ฝังผ่าน `@fontsource/ibm-plex-sans-thai` (import ใน `main.js`) — **ห้ามเรียกฟอนต์/CSS/ไอคอนจาก CDN** ทุกอย่างต้อง bundle ในโปรเจกต์ (ใช้งานออฟไลน์ 100%)
- ไอคอนใช้ `@ant-design/icons-vue` เท่านั้น (bundle อยู่แล้ว)
- อยากปรับธีมทั้งระบบ = แก้ที่ `theme.js` + `tokens.css` ที่เดียว ห้ามไล่แก้รายหน้า
- สี Klein blue หลัก = `#002fa7`, ฟ้าเน้น = `#1677ff`, พื้นหน้า = `#eef1f7`, ตัวอักษร = `#1a2340`

## กฎ Migration ไป Vuetify 3 (บังคับ / permanent, ตั้งแต่ 2026-08-08)

**กำลังย้าย component library หลักจาก ant-design-vue ไปเป็น Vuetify 3 แบบค่อยเป็นค่อยไปทีละโมดูล** (ตาม user ตัดสินใจ ไม่ทำทีเดียวทั้งแอปเพราะเสี่ยงเกินไป) — เริ่มจากโมดูล **AIA-GO ก่อน**

- ระหว่างเปลี่ยนผ่าน **ทั้งสอง library อยู่ร่วมกันในแอปเดียว** — โมดูลที่ยังไม่ย้าย (8 โมดูล admin, Patient Encounter, Login) ยังคงใช้ ant-design-vue + Klein Pro (`theme.js`/`tokens.css`) ตามเดิมทุกกฎข้างบน ห้ามแตะ
- โมดูล/ไฟล์ที่ย้ายไป Vuetify แล้วเท่านั้นที่ใช้กฎชุดนี้แทน
- Vuetify theme ต้องแมปสีให้ตรงกับ Klein Pro tokens เดิม (`--mc-primary` #002fa7, `--mc-accent` #1677ff, `--mc-canvas` #eef1f7, `--mc-ink` #1a2340, success/warning/info/error ชุดเดิม) — ตั้งค่าที่ **`client/src/vuetify.js`** (แหล่งความจริงของฝั่ง Vuetify คู่กับ `theme.js` เดิมของฝั่ง antd)
- ห้าม hardcode สี/ฟอนต์/มุมโค้งเหมือนเดิม แม้เปลี่ยน library — ใช้ Vuetify theme token (`primary`, `on-surface` ฯลฯ) หรือ CSS var `--mc-*` เดิมก็ได้ถ้าจำเป็น
- ฟอนต์ยังคงเป็น IBM Plex Sans Thai เดิม, ไอคอนยังคง bundle ในโปรเจกต์ 100% ไม่โหลดจาก CDN (Vuetify ใช้ Material Design Icons ผ่าน `@mdi/font` ที่ bundle ในโปรเจกต์ ไม่ใช่ animated icon จากเว็บภายนอกอย่าง Flaticon)
- ลำดับ progress: AiaLangSwitch → AiaStatCards → AiaInterface (container) → 6 panel ทีละตัว (Check/Register/Discharge/Claim/ClaimStatus/Billing+PackModal) → App.vue shell (navigation drawer) → AiaInterface's tab bar (v-bottom-navigation) — ดู `log/2026-08-08.md` และ TaskList สำหรับสถานะล่าสุด
- **App.vue's shell (side navigation drawer) เป็น Vuetify แล้ว** (`<v-app>` + `<v-navigation-drawer rail permanent>` แทน hamburger + `a-drawer` เดิม) — เป็นข้อยกเว้นเดียวที่แตะ "โครงนอก" ของทุกโมดูลรวมถึง 8 โมดูล admin ที่ยังไม่ migrate เพราะ shell (header/menu) ใช้ร่วมกันทั้งแอป **แต่เนื้อหา/logic ข้างในแต่ละโมดูล admin ที่ยังไม่ migrate ยังเป็น ant-design-vue เดิมทุกอย่าง ไม่ได้ถูกแตะ** — สลับ library ได้เพราะ `<v-main>` ห่อ antd content ได้ปกติ ไม่ต้อง migrate เนื้อหาไปพร้อมกัน
- โมดูลอื่นนอกจาก AIA-GO (เนื้อหาข้างในแต่ละหน้า ไม่ใช่ shell) **ยังไม่ต้องแตะ** จนกว่า user จะสั่งให้ย้ายต่อ
