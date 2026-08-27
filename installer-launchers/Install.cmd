@echo off
setlocal
cd /d "%~dp0"

call "%~dp0PublisherStudio.Setup.exe" --install-publisherstudio --start-publisherstudio --shortcuts --port 58071
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo PublisherStudio setup failed with exit code %EXITCODE%.
) else (
    echo PublisherStudio install/start finished.
)

echo.
pause
exit /b %EXITCODE%
