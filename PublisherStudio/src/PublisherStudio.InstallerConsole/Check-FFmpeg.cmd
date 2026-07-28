@echo off
setlocal
cd /d "%~dp0"
call "%~dp0PublisherStudio.Setup.exe" --check-ffmpeg
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
