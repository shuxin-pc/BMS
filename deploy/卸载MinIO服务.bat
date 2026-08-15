@echo off
chcp 65001 >nul
setlocal

rem ============================================================
rem  卸载 MinIO 服务（不删除数据目录）
rem  必须右键「以管理员身份运行」
rem  用于重新注册前的清理，或不再以服务方式运行时
rem ============================================================

set "MINIO_HOME=D:\软件\MinIO"

net session >nul 2>&1
if errorlevel 1 (
    echo [错误] 需要管理员权限。请右键本文件，选择「以管理员身份运行」。
    pause
    exit /b 1
)

echo 正在停止并移除 MinIO 服务...
"%MINIO_HOME%\nssm.exe" stop MinIO
"%MINIO_HOME%\nssm.exe" remove MinIO confirm

echo.
echo 已移除服务。数据目录未删除，重新注册后数据仍可用。
pause
