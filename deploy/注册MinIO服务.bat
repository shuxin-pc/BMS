@echo off
chcp 65001 >nul
setlocal

rem ============================================================
rem  把 MinIO 注册为 Windows 服务（开机自启、崩溃自动重启）
rem  必须右键「以管理员身份运行」
rem  只需执行一次；重复执行前请先跑 卸载MinIO服务.bat
rem ============================================================

set "MINIO_HOME=D:\软件\MinIO"
rem 数据目录不能放系统盘：C 盘剩余空间不足时 MinIO 会以「minimum free drive threshold」拒绝一切写入
set "MINIO_DATA=D:\minio-data"
set "MINIO_USER=mes"
set "MINIO_PASSWORD=mes@2026"

rem 校验管理员权限：net session 在非提权会话下必然失败
net session >nul 2>&1
if errorlevel 1 (
    echo [错误] 需要管理员权限。请右键本文件，选择「以管理员身份运行」。
    pause
    exit /b 1
)

if not exist "%MINIO_HOME%\nssm.exe" (
    echo [错误] 未找到 %MINIO_HOME%\nssm.exe
    pause
    exit /b 1
)
if not exist "%MINIO_HOME%\minio.exe" (
    echo [错误] 未找到 %MINIO_HOME%\minio.exe
    pause
    exit /b 1
)

if not exist "%MINIO_DATA%" mkdir "%MINIO_DATA%"
if not exist "%MINIO_HOME%\logs" mkdir "%MINIO_HOME%\logs"

echo 正在注册 MinIO 服务...
"%MINIO_HOME%\nssm.exe" install MinIO "%MINIO_HOME%\minio.exe"
if errorlevel 1 (
    echo [错误] 注册失败。若提示服务已存在，请先执行 卸载MinIO服务.bat
    pause
    exit /b 1
)

"%MINIO_HOME%\nssm.exe" set MinIO AppDirectory "%MINIO_HOME%"
"%MINIO_HOME%\nssm.exe" set MinIO AppParameters "server \"%MINIO_DATA%\" --address :9000 --console-address :9001"

rem 凭据由 nssm 注入进程环境：既不出现在进程命令行（进程列表可见），
rem 也不依赖 setx（服务以 SYSTEM 运行，读不到用户级环境变量）
"%MINIO_HOME%\nssm.exe" set MinIO AppEnvironmentExtra MINIO_ROOT_USER=%MINIO_USER% MINIO_ROOT_PASSWORD=%MINIO_PASSWORD%

rem 日志落盘：服务无控制台窗口，不落盘则出问题无从查起
"%MINIO_HOME%\nssm.exe" set MinIO AppStdout "%MINIO_HOME%\logs\minio.log"
"%MINIO_HOME%\nssm.exe" set MinIO AppStderr "%MINIO_HOME%\logs\minio-err.log"
"%MINIO_HOME%\nssm.exe" set MinIO AppRotateFiles 1
"%MINIO_HOME%\nssm.exe" set MinIO AppRotateBytes 10485760

"%MINIO_HOME%\nssm.exe" set MinIO AppExit Default Restart
"%MINIO_HOME%\nssm.exe" set MinIO Start SERVICE_AUTO_START
"%MINIO_HOME%\nssm.exe" set MinIO Description "MinIO 对象存储（BMS 图片存储）"

echo.
echo 正在启动服务...
"%MINIO_HOME%\nssm.exe" start MinIO

echo.
echo ==== 服务状态 ====
sc query MinIO | "%SystemRoot%\System32\findstr.exe" "STATE"

echo.
echo 注册完成。请按以下命令验证（普通终端即可）：
echo    cd /d %MINIO_HOME%
echo    mc.exe alias set local http://localhost:9000 %MINIO_USER% %MINIO_PASSWORD%
echo    mc.exe admin info local
echo.
pause
