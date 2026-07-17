@echo off
setlocal
cd /d "%~dp0"
call "%~dp0Setup\PublisherStudio.Setup.exe" --start --install-dir "%~dp0"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" echo PublisherStudio.Setup start failed with exit code %EXITCODE%.
if not "%EXITCODE%"=="0" pause
exit /b %EXITCODE%
