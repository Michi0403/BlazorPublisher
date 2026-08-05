@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Update-GitHubPagesSnapshot.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" pause
exit /b %EXITCODE%
