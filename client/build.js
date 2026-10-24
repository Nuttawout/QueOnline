// Cloudflare Pages build: the page is already built (site/), copy it to dist/ — nothing to install or compile.
import { cpSync, rmSync, mkdirSync } from 'node:fs'
rmSync('dist', { recursive: true, force: true })
mkdirSync('dist', { recursive: true })
cpSync('site', 'dist', { recursive: true })
console.log('dist ready')
