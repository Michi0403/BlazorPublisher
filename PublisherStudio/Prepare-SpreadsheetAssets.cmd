@echo off
setlocal
cd /d "%~dp0"
call "%~dp0Prepare-DevExpressAssets.cmd" %*
exit /b %ERRORLEVEL%
