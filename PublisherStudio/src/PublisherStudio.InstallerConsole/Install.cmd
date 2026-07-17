@echo off
setlocal
cd /d "%~dp0"

set "SETUP_EXE=%~dp0PublisherStudio.Setup.exe"
if not exist "%SETUP_EXE%" for /r "%~dp0" %%F in (PublisherStudio.Setup.exe) do if not defined SETUP_EXE_FOUND set "SETUP_EXE=%%~fF"& set "SETUP_EXE_FOUND=1"
if not exist "%SETUP_EXE%" (
    echo PublisherStudio.Setup.exe was not found below "%~dp0".
    pause
    exit /b 2
)

echo Starting: "%SETUP_EXE%" --install --start --port 0 %*
call "%SETUP_EXE%" --install --start --port 0 %*
set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
    echo PublisherStudio.Setup failed with exit code %EXITCODE%.
    pause
)
exit /b %EXITCODE%
