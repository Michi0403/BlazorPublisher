@echo off
setlocal
cd /d "%~dp0"
call "%~dp0PublisherStudio.Setup.exe" --install-ffmpeg
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
