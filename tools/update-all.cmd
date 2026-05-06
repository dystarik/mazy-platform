@echo off
setlocal

where pwsh >nul 2>&1
if errorlevel 1 (
    echo pwsh not found. Install PowerShell 7 and try again.
    pause
    exit /b 1
)

pwsh -NoLogo -NoProfile -NoExit -ExecutionPolicy Bypass -File "%~dp0packages\update-all.ps1" %*
