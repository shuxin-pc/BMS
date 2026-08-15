@echo off
chcp 65001 >nul
cd /d "%~dp0"

rem MinIO 与 PostgreSQL 均已注册为 Windows 服务、开机自启，本脚本无需处理。
rem MinIO 服务的注册/卸载见 deploy\注册MinIO服务.bat（一次性运维脚本）。

echo [1/2] 启动网关（http://localhost:5020）...
start "Bms Gateway" dotnet run --project src/Gateway/Bms.Gateway.Api

echo [2/2] 启动前端（http://localhost:3000）...
cd /d "%~dp0src\Admin\mes-admin"
npm run dev

pause
