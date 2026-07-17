@echo off
setlocal
cd /d "%~dp0"

call "%~dp0PublisherStudio.Setup.exe" --uninstall --force-delete
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo BlazorPublisher uninstall failed with exit code %EXITCODE%.
) else (
    echo BlazorPublisher uninstall finished.
)

echo.
pause
exit /b %EXITCODE%
