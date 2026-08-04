@echo off
setlocal
cd /d "%~dp0"

rem A previous setup may have staged this repair copy because Windows cannot overwrite
rem the setup executable while it is running. Promote it before invoking setup.
set "SETUP_EXE=%~dp0PublisherStudio.Setup.exe"
set "SETUP_REPAIR=%~dp0PublisherStudio.Setup.repair.exe"
if exist "%SETUP_REPAIR%" (
    copy /b /y "%SETUP_REPAIR%" "%SETUP_EXE%.incoming" >nul
    if errorlevel 1 goto setup_repair_failed
    move /y "%SETUP_EXE%.incoming" "%SETUP_EXE%" >nul
    if errorlevel 1 goto setup_repair_failed
    del /q "%SETUP_REPAIR%" >nul 2>&1
)

rem Preservation-first install: runtime files are merged in place; setup launchers are repaired/replaced.
call "%SETUP_EXE%" --install-blazorpublisher --install-ffmpeg --start-blazorpublisher --port 58071 --shortcuts
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%

:setup_repair_failed
echo PublisherStudio setup could not promote the staged repair executable.
echo Close any running PublisherStudio setup process and run this launcher again.
pause
exit /b 1
