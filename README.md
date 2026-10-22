# MediQ Online

Patient queue tracking site (read-only): `/track/<hospital code>/<tracking code>`.
Reads what the hospitals park in the AIA-GO staging database (schema `MediQ`) and serves the tracking page.

See `คู่มือติดตั้ง.txt` for installation (Node.js 22, `npm ci --omit=dev`, `.env`, HTTPS / Cloudflare Tunnel).
`track/` is the built tracking page (static). No secrets are stored here: copy `.env.example` to `.env` on the server.
