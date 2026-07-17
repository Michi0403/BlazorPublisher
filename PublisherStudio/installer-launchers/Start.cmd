@echo off
setlocal
set "SEARCH_ROOT=%~dp0"
set "SETUP_EXE="

if exist "%SEARCH_ROOT%PublisherStudio.Setup.exe" set "SETUP_EXE=%SEARCH_ROOT%PublisherStudio.Setup.exe"
if not defined SETUP_EXE if exist "%SEARCH_ROOT%Setup\PublisherStudio.Setup.exe" set "SETUP_EXE=%SEARCH_ROOT%Setup\PublisherStudio.Setup.exe"
if not defined SETUP_EXE for /r "%SEARCH_ROOT%" %%F in (PublisherStudio.Setup.exe) do if not defined SETUP_EXE set "SETUP_EXE=%%~fF"

if not defined SETUP_EXE (
    echo PublisherStudio.Setup.exe was not found below "%SEARCH_ROOT%".
    pause
    exit /b 2
)

echo Starting: "%SETUP_EXE%" --start --port 0 %*
call "%SETUP_EXE%" --start --port 0 %*
set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
    echo BlazorPublisher start failed with exit code %EXITCODE%.
    pause
)
exit /b %EXITCODE%
