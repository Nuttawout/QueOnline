// Access to the online database (MediQ_Online). The app logs in with an account that can only read, and call two procedures.
import sql from 'mssql'

let pool
// where the tables live: dbo in the online database of the HTTPS way, MediQ in the AIA-GO database of the pull way (ONLINE_SQL_SCHEMA=MediQ)
const SCHEMA = /^[A-Za-z0-9_]+$/.test(process.env.ONLINE_SQL_SCHEMA || '') ? process.env.ONLINE_SQL_SCHEMA : 'dbo'
const TABLE = `${SCHEMA}.PublicQueueVisit`

export async function connect () {
  pool = await new sql.ConnectionPool({
    server: process.env.ONLINE_SQL_SERVER,
    port: process.env.ONLINE_SQL_PORT ? Number(process.env.ONLINE_SQL_PORT) : undefined,
    database: process.env.ONLINE_SQL_DATABASE || 'MediQ_Online',
    user: process.env.ONLINE_SQL_USER,
    password: process.env.ONLINE_SQL_PASSWORD,
    options: { encrypt: process.env.ONLINE_SQL_ENCRYPT !== 'false', trustServerCertificate: process.env.ONLINE_SQL_TRUST_CERT === 'true' },
    pool: { max: 10, min: 0, idleTimeoutMillis: 30000 },
    connectionTimeout: 10000,
    requestTimeout: 15000
  }).connect()
}

export const close = () => pool?.close()
export const ping = async () => { await pool.request().query('SELECT 1 AS ok') }

// accepted = rows that were new or newer than what we had (older / repeated snapshots are ignored by the procedure)
export async function upsertVisits (hospitalCode, visits) {
  const result = await pool.request().input('hospitalCode', sql.VarChar(10), hospitalCode).input('visitsJson', sql.NVarChar(sql.MAX), JSON.stringify(visits)).execute(`${SCHEMA}.UpsertPublicQueueVisit`)
  return result.recordset[0].AcceptedRowCount
}

export async function purgeOld (keepDays = 2) {
  const result = await pool.request().input('keepDays', sql.Int, keepDays).execute(`${SCHEMA}.PurgeOldPublicQueueVisit`)
  return result.recordset[0].DeletedRowCount
}

const COLUMNS = `CONVERT(char(10), ServiceDate, 23) AS serviceDate, VisitCode, QueueNumber, CurrentStepIndex, StepCount, CurrentDepartmentName,
  CurrentStatusCode, CurrentCounterName, CalledDoctorName, RequestedDoctorName, ColorHexCode, QueuePosition, EstimatedWaitMinutes,
  JourneyStepsJson, CONVERT(varchar(23), VisitCreatedAt, 126) AS visitCreatedAt, CONVERT(varchar(23), CalledAt, 126) AS calledAt`

const toMs = (text) => (text ? Date.parse(text + '+07:00') : null) // stored as Bangkok wall time

// the same shape the hospital's own tracking API returns, so the tracking page works unchanged
export function toPublicTicket (r) {
  return {
    id: r.VisitCode,
    code: r.VisitCode,
    number: r.QueueNumber,
    departmentName: r.CurrentDepartmentName,
    color: r.ColorHexCode,
    status: r.CurrentStatusCode,
    counter: r.CurrentCounterName,
    doctorName: r.CalledDoctorName,
    requestedDoctorName: r.RequestedDoctorName,
    createdAt: toMs(r.visitCreatedAt),
    calledAt: toMs(r.calledAt),
    stepIndex: r.CurrentStepIndex,
    steps: JSON.parse(r.JourneyStepsJson),
    position: r.QueuePosition,
    estimatedWaitMinutes: r.EstimatedWaitMinutes
  }
}

export async function findByCode (hospitalCode, code) {
  const r = await pool.request().input('hospital', sql.VarChar(10), hospitalCode).input('code', sql.Char(6), code).query(`SELECT TOP 1 ${COLUMNS} FROM ${TABLE} WHERE HospitalCode = @hospital AND VisitCode = @code ORDER BY ServiceDate DESC`)
  return r.recordset[0] ? toPublicTicket(r.recordset[0]) : null
}

export async function findTodayByNumber (hospitalCode, number) {
  const r = await pool.request().input('hospital', sql.VarChar(10), hospitalCode).input('number', sql.VarChar(10), number)
    .query(`SELECT TOP 1 ${COLUMNS} FROM ${TABLE} WHERE HospitalCode = @hospital AND QueueNumber = @number ORDER BY ServiceDate DESC, VisitCreatedAt DESC`)
  return r.recordset[0] ? toPublicTicket(r.recordset[0]) : null
}

// the current view of several visits at once (to push to the people watching them)
export async function findMany (hospitalCode, codes) {
  if (!codes.length) return []
  const r = await pool.request().input('hospital', sql.VarChar(10), hospitalCode).input('codes', sql.NVarChar(sql.MAX), JSON.stringify(codes)).query(`
    SELECT ${COLUMNS} FROM ${TABLE} p
    WHERE HospitalCode = @hospital AND VisitCode IN (SELECT value FROM OPENJSON(@codes))
      AND ServiceDate = (SELECT MAX(ServiceDate) FROM ${TABLE} q WHERE q.HospitalCode = p.HospitalCode AND q.VisitCode = p.VisitCode)`)
  return r.recordset.map(toPublicTicket)
}
