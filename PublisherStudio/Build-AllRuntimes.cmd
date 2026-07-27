@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-AllRuntimes.ps1" %*
