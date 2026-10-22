@echo off
rem MediQ Online. First time: run  npm ci --omit=dev  in this folder, copy .env.example to .env and fill it in.
cd /d "%~dp0"
set ONLINE_PUBLIC_DIST=%~dp0track
node src\index.js
pause
