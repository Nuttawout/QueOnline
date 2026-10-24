# Qdee tracking page (queonline)

Static patient queue-tracking page of **Qdee (คิวดี)**, served by Cloudflare Pages.

- `client/site/` is the built page (`config.json` holds the API address: `apiUrl`). Edit `config.json` to point at another API.
- `client/build.js` just copies `site/` to `dist/` (Pages: root directory `client`, build command `npm run build`, output `dist`).
- The page calls the Qdee online API; when the API cannot be reached it shows a notice and keeps retrying.

The previous site is kept as the tag `old-queonline`.
