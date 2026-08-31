@echo off
setlocal
chcp 65001 >nul
pushd "%~dp0"
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Build-AllRuntimes.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
  echo.
  echo PublisherStudio all-runtime build failed with exit code %EXITCODE%.
  echo Review the first error above. This window will remain open.
  pause
)
popd
exit /b %EXITCODE%
