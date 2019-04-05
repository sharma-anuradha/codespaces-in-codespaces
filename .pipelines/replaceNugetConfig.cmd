@echo off
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "&'%~dpn0.ps1' %*"
exit /B %ERRORLEVEL%
