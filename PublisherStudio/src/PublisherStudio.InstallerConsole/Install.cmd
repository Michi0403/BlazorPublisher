@echo off
setlocal
cd /d "%~dp0"

rem Preservation-first install: existing LocalAppData state is merged, never deleted.
call "%~dp0PublisherStudio.Setup.exe" --install-blazorpublisher --install-ffmpeg --start-blazorpublisher --port 58071 --shortcuts
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
