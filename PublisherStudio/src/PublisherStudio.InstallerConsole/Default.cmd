@echo off
setlocal
cd /d "%~dp0"

rem No arguments intentionally run the preservation-first default install/update routine.
call "%~dp0PublisherStudio.Setup.exe"
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
