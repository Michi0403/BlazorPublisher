@echo off
setlocal
cd /d "%~dp0"
call "%~dp0PublisherStudio.Setup.exe" --update --start
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" echo PublisherStudio.Setup.exe update failed with exit code %EXITCODE%.
if not "%EXITCODE%"=="0" pause
exit /b %EXITCODE%
