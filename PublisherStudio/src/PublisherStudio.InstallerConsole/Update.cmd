@echo off
setlocal
cd /d "%~dp0"

call "%~dp0PublisherStudio.Setup.exe" --update-blazorpublisher --start-blazorpublisher --port 58071 --shortcuts
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo PublisherStudio.Setup failed with exit code %EXITCODE%.
) else (
    echo BlazorPublisher update/start finished.
)

echo.
pause
exit /b %EXITCODE%
