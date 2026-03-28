@echo off
cd /d "%~dp0"
set SCRIPT_DIR=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%ArmBuild.ps1"
pause