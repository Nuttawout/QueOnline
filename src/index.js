// MediQ Online — the internet-facing patient tracking site.
//
//   hospital ──(signed HTTPS push)──▶ POST /api/ingest/visits ──▶ MediQ_Online ──▶ GET /api/tickets/<code> + realtime ──▶ patient's phone
//
// It only ever receives what patients may see (no name, HN or phone) and never talks back to the hospital.
import './env.js'
import express from 'express'
import http from 'node:http'
import crypto from 'node:crypto'
import fs from 'node:fs'
import path from 'node:path'
import { Server } from 'socket.io'
import * as db from './db.js'

const PORT = process.env.PORT || 3100
// Every hospital / branch that may send data, with its own secret: ONLINE_HOSPITALS="24191:<secret>,40750:<secret>" (hospital code : secret).
// The old single-hospital settings (ONLINE_SYNC_SITE + ONLINE_SYNC_SECRET) still work as one hospital.
const HOSPITALS = new Map()
for (const pair of String(process.env.ONLINE_HOSPITALS || '').split(',')) {
  const i = pair.indexOf(':')
  const code = pair.slice(0, i).trim()
  const secret = pair.slice(i + 1).trim()
  if (i > 0 && /^[A-Za-z0-9]{1,10}$/.test(code)) HOSPITALS.set(code, secret)
}
if (!HOSPITALS.size && process.env.ONLINE_SYNC_SECRET) HOSPITALS.set(process.env.ONLINE_SYNC_SITE || 'hospital', process.env.ONLINE_SYNC_SECRET)
const HOSPITAL_CODE = /^[A-Za-z0-9]{1,10}$/
// the code a link without a hospital in it belongs to: only when exactly one hospital sends here
const onlyHospital = HOSPITALS.size === 1 ? [...HOSPITALS.keys()][0] : (process.env.ONLINE_DEFAULT_HOSPITAL && HOSPITAL_CODE.test(process.env.ONLINE_DEFAULT_HOSPITAL) ? process.env.ONLINE_DEFAULT_HOSPITAL : null)
const ALLOW_NUMBER_LOOKUP = process.env.ALLOW_NUMBER_LOOKUP === 'true'
const MAX_CLOCK_SKEW_MS = 5 * 60 * 1000 // a signed request older than this is refused (replay protection)
const STATUSES = ['waiting', 'called', 'serving', 'done', 'skipped', 'cancelled']

if ([...HOSPITALS.values()].some(s => s.length < 16)) {
  console.error('Each secret in ONLINE_HOSPITALS="<hospital code>:<secret>,..." must be a long random value (at least 16 characters).')
  process.exit(1)
}
// Without any hospital listed this site only READS what the hospitals have parked in the AIA-GO database (ONLINE_SQL_SCHEMA=MediQ, a read-only login):
// nobody can send to it, and every hospital code that has rows there is served.
console.log(HOSPITALS.size ? `🏥 hospitals allowed to send by HTTPS: ${[...HOSPITALS.keys()].join(', ')}` : '🏥 read-only: serving what the hospitals park in the database (no HTTPS ingest)')

await db.connect()
console.log(`🌐 MediQ Online: connected to ${process.env.ONLINE_SQL_SERVER}/${process.env.ONLINE_SQL_DATABASE || 'MediQ_Online'}`)

const app = express()
if (process.env.TRUST_PROXY) app.set('trust proxy', Number(process.env.TRUST_PROXY) || 1)
app.disable('x-powered-by')
app.use((req, res, next) => {
  res.set({ 'X-Content-Type-Options': 'nosniff', 'Referrer-Policy': 'no-referrer', 'X-Frame-Options': 'DENY' })
  next()
})

const server = http.createServer(app)
const io = new Server(server) // same origin as the page: no CORS

const wrap = fn => (req, res, next) => Promise.resolve(fn(req, res, next)).catch(next)

// ---------- a small per-address rate limit for the public endpoints ----------
function rateLimit (max, windowMs) {
  const hits = new Map()
  setInterval(() => { const now = Date.now(); for (const [k, v] of hits) if (v.reset < now) hits.delete(k) }, windowMs).unref()
  return (req, res, next) => {
    const now = Date.now()
    const h = hits.get(req.ip)
    if (!h || h.reset < now) { hits.set(req.ip, { count: 1, reset: now + windowMs }); return next() }
    if (++h.count > max) return res.status(429).json({ error: 'ส่งคำขอถี่เกินไป กรุณารอสักครู่' })
    next()
  }
}

// ---------- ingest: the only way data comes in ----------
class BadPayload extends Error {}

const str = (v, max, { required = true, pattern } = {}) => {
  if (v === null || v === undefined || v === '') { if (required) throw new BadPayload('missing field'); return null }
  if (typeof v !== 'string' || v.length > max || (pattern && !pattern.test(v))) throw new BadPayload('invalid field')
  return v
}
const int = (v, min, max) => { if (!Number.isInteger(v) || v < min || v > max) throw new BadPayload('invalid number'); return v }
const when = (v, required = true) => str(v, 23, { required, pattern: /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,3})?$/ })

// Accept only exactly the fields a patient may see, whatever else the sender includes
function cleanVisit (v) {
  if (!v || typeof v !== 'object') throw new BadPayload('invalid visit')
  if (!Array.isArray(v.journeySteps) || v.journeySteps.length < 1 || v.journeySteps.length > 20) throw new BadPayload('invalid steps')
  if (!Number.isSafeInteger(v.snapshotVersion) || v.snapshotVersion < 1) throw new BadPayload('invalid version')
  const status = str(v.currentStatusCode, 12)
  if (!STATUSES.includes(status)) throw new BadPayload('invalid status')
  return {
    serviceDate: str(v.serviceDate, 10, { pattern: /^\d{4}-\d{2}-\d{2}$/ }),
    visitCode: str(v.visitCode, 6, { pattern: /^[0-9A-Fa-f]{6}$/ }).toUpperCase(),
    queueNumber: str(v.queueNumber, 10, { pattern: /^[A-Za-z0-9]{1,10}$/ }),
    snapshotVersion: v.snapshotVersion,
    currentStepIndex: int(v.currentStepIndex, 0, 50),
    stepCount: int(v.stepCount, 1, 50),
    currentDepartmentName: str(v.currentDepartmentName, 60),
    currentStatusCode: status,
    currentCounterName: str(v.currentCounterName, 40, { required: false }),
    calledDoctorName: str(v.calledDoctorName, 60, { required: false }),
    requestedDoctorName: str(v.requestedDoctorName, 60, { required: false }),
    colorHexCode: str(v.colorHexCode, 7, { pattern: /^#[0-9A-Fa-f]{6}$/ }),
    queuePosition: int(v.queuePosition, 0, 100000),
    estimatedWaitMinutes: int(v.estimatedWaitMinutes, 0, 100000),
    journeySteps: v.journeySteps.map(s => ({
      id: str(s?.id, 5), name: str(s?.name, 60), icon: str(s?.icon ?? '', 8, { required: false }) ?? '',
      state: (() => { if (!['done', 'current', 'pending'].includes(s?.state)) throw new BadPayload('invalid step'); return s.state })()
    })),
    visitCreatedAt: when(v.visitCreatedAt),
    calledAt: when(v.calledAt, false)
  }
}

app.post('/api/ingest/visits', express.json({ limit: '2mb', verify: (req, res, buf) => { req.rawBody = buf } }), wrap(async (req, res) => {
  const timestamp = req.get('x-mediq-timestamp')
  const signature = req.get('x-mediq-signature') || ''
  const hospital = String(req.get('x-mediq-site') || '')
  const SECRET = HOSPITALS.get(hospital) // the hospital code says which secret to check; it is only trusted once the signature is right
  if (!SECRET || !timestamp || !req.rawBody) // (no hospitals listed = the read-only way: nothing is accepted here) return res.status(401).json({ error: 'unauthorized' })
  if (!(Math.abs(Date.now() - Number(timestamp)) <= MAX_CLOCK_SKEW_MS)) return res.status(401).json({ error: 'request too old or clock is wrong' })
  const expected = crypto.createHmac('sha256', SECRET).update(`${timestamp}.`).update(req.rawBody).digest()
  const given = Buffer.from(/^[0-9a-f]+$/i.test(signature) ? signature : '', 'hex')
  if (given.length !== expected.length || !crypto.timingSafeEqual(given, expected)) return res.status(401).json({ error: 'unauthorized' })

  let visits
  try {
    if (!Array.isArray(req.body?.visits) || req.body.visits.length > 200) throw new BadPayload('visits must be an array of at most 200')
    visits = req.body.visits.map(cleanVisit)
  } catch (e) {
    if (e instanceof BadPayload) return res.status(400).json({ error: e.message })
    throw e
  }
  const accepted = visits.length ? await db.upsertVisits(hospital, visits) : 0

  // realtime: tell whoever is watching one of these visits (read back, so they get exactly what is stored)
  const watched = visits.map(v => v.visitCode).filter(code => io.sockets.adapter.rooms.get(`ticket:${hospital}:${code}`)?.size)
  if (watched.length) for (const t of await db.findMany(hospital, [...new Set(watched)])) io.to(`ticket:${hospital}:${t.code}`).emit('ticket:update', t)

  res.json({ received: visits.length, accepted })
}))

// ---------- reading: patients, by their private tracking code ----------
const readLimit = rateLimit(60, 60_000)

// /api/tickets/<hospital code>/<tracking code>  (a link without the hospital code works only when one hospital sends here)
async function readTicket (hospital, rawKey) {
  const key = String(rawKey).trim()
  if (!hospital || !HOSPITAL_CODE.test(hospital)) return null
  if (/^[0-9A-Fa-f]{6}$/.test(key)) return db.findByCode(hospital, key.toUpperCase())
  if (ALLOW_NUMBER_LOOKUP && /^[A-Za-z0-9]{1,10}$/.test(key)) return db.findTodayByNumber(hospital, key.toUpperCase())
  return null
}
const notFound = (res) => res.status(404).json({ error: 'ไม่พบคิว กรุณาตรวจสอบรหัสติดตามบนบัตรคิว' })
app.get('/api/tickets/:hospital/:key', readLimit, wrap(async (req, res) => {
  const ticket = HOSPITAL_CODE.test(req.params.hospital) ? await readTicket(req.params.hospital, req.params.key) : null
  if (!ticket) return notFound(res)
  res.json(ticket)
}))
app.get('/api/tickets/:key', readLimit, wrap(async (req, res) => {
  const ticket = await readTicket(onlyHospital, req.params.key)
  if (!ticket) return notFound(res)
  res.json(ticket)
}))

app.get('/api/health', wrap(async (req, res) => {
  await db.ping()
  res.json({ ok: true, time: Date.now() })
}))

io.on('connection', socket => {
  const watching = new Set()
  // the page says { hospital, code } (a plain code = the only hospital)
  const parse = (arg) => {
    const hospital = String(typeof arg === 'object' && arg ? arg.hospital || onlyHospital || '' : onlyHospital || '')
    const code = String(typeof arg === 'object' && arg ? arg.code : arg || '').toUpperCase()
    return HOSPITAL_CODE.test(hospital) && /^[0-9A-F]{6}$/.test(code) ? { room: `ticket:${hospital}:${code}`, hospital, code } : null
  }
  socket.on('track', async (arg) => {
    const t = parse(arg)
    if (!t || (!watching.has(t.room) && watching.size >= 5)) return
    try {
      const ticket = await db.findByCode(t.hospital, t.code)
      if (!ticket) return
      watching.add(t.room)
      socket.join(t.room)
      socket.emit('ticket:update', ticket)
    } catch { /* the page keeps working from its own fetches */ }
  })
  socket.on('untrack', (arg) => { const t = parse(arg); if (t) { watching.delete(t.room); socket.leave(t.room) } })
})

// ---------- realtime for the PULL way: the hospital writes into the staging database, so the people watching a visit are served by looking again ----------
// (every few seconds, only for the visits somebody is watching, and only what changed is sent)
const lastSent = new Map() // room -> what was last sent
setInterval(async () => {
  try {
    const byHospital = new Map()
    for (const [room, members] of io.sockets.adapter.rooms) {
      if (!room.startsWith('ticket:') || !members.size) continue
      const [, hospital, code] = room.split(':')
      if (!byHospital.has(hospital)) byHospital.set(hospital, [])
      byHospital.get(hospital).push(code)
    }
    for (const room of [...lastSent.keys()]) if (!io.sockets.adapter.rooms.get(room)?.size) lastSent.delete(room)
    for (const [hospital, codes] of byHospital) {
      for (const t of await db.findMany(hospital, codes)) {
        const room = `ticket:${hospital}:${t.code}`
        const sig = JSON.stringify(t)
        if (lastSent.get(room) === sig) continue
        lastSent.set(room, sig)
        io.to(room).emit('ticket:update', t)
      }
    }
  } catch (e) { console.error('[online] watch poll failed:', e.message) }
}, Math.max(1000, Number(process.env.ONLINE_PULL_INTERVAL_MS) || 2000)).unref()

// ---------- the tracking page itself ----------
const dist = path.resolve(process.env.ONLINE_PUBLIC_DIST || '../frontend/dist-standalone/track')
if (fs.existsSync(path.join(dist, 'index.html'))) {
  app.use(express.static(dist, { index: false, maxAge: '1h' }))
  app.use('/_apps/track', express.static(dist, { index: false, maxAge: '1h' }))
  app.get(/^\/(?!api|socket\.io).*/, (req, res) => res.sendFile(path.join(dist, 'index.html')))
} else {
  console.warn(`(no tracking page found in ${dist}: only the API is served. Build it with: cd ../frontend && npm run build:standalone -- track)`)
}

app.use((err, req, res, next) => {
  if (err.type === 'entity.parse.failed') return res.status(400).json({ error: 'invalid JSON' })
  if (err.type === 'entity.too.large') return res.status(413).json({ error: 'too large' })
  console.error('[online]', err.message)
  res.status(500).json({ error: 'เกิดข้อผิดพลาด ลองใหม่อีกครั้ง' })
})

// keep only the last couple of days
const purge = () => db.purgeOld(2).then(n => n && console.log(`purged ${n} old visits`)).catch(e => console.error('[online] purge failed:', e.message))
purge()
setInterval(purge, 6 * 3600 * 1000).unref()

server.listen(PORT, () => console.log(`🌐 MediQ Online listening on http://localhost:${PORT}`))
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, async () => { await db.close(); process.exit(0) })
