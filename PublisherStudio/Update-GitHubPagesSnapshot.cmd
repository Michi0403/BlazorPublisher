@echo off
setlocal
pushd "%~dp0"
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0build\Update-GitHubPagesSnapshot.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
  echo.
  echo PublisherStudio GitHub Pages snapshot update failed with exit code %EXITCODE%.
  echo Review the first error above. This window will remain open.
  pause
)
popd
exit /b %EXITCODE%
