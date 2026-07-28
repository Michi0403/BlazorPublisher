@echo off
setlocal
cd /d "%~dp0"
call "%~dp0PublisherStudio.Setup.exe" --update-blazorpublisher --install-ffmpeg --start-blazorpublisher --port 58071 --shortcuts
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
