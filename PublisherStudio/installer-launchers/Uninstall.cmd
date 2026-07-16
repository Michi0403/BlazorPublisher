@echo off
setlocal
cd /d "%~dp0"

call "%~dp0PublisherStudio.Setup.exe" --uninstall --force
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo PublisherStudio.Setup.exe Uninstall failed with exit code %EXITCODE%.
) else (
    echo PublisherStudio.Setup.exe Uninstall finished.
)

echo.
pause
exit /b %EXITCODE%