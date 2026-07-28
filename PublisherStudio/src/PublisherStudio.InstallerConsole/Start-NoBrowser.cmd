@echo off
setlocal
cd /d "%~dp0"
call "%~dp0PublisherStudio.Setup.exe" --start-blazorpublisher --no-browser --port 58071
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
