@echo off
setlocal
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Update-FrontendIntegrity.ps1"
if errorlevel 1 exit /b %errorlevel%
echo PublisherStudio frontend integrity manifest refreshed and validated.
