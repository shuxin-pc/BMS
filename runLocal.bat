@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo 启动网关（http://localhost:5020）...
start "Bms Gateway" dotnet run --project src/Gateway/Bms.Gateway.Api

echo 启动前端（http://localhost:3000）...
cd /d "%~dp0src\Admin\mes-admin"
npm run dev

pause
