@echo off
setlocal
cd /d "%~dp0"

call "%~dp0PublisherStudio.Setup.exe" --start
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo PublisherStudio.Setup.exe failed with exit code %EXITCODE%.
) else (
    echo PublisherStudio.Setup.exe start finished.
)

echo.
pause
exit /b %EXITCODE%