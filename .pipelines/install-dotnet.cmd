@echo off
:: install-dotnet.cmd
echo Running %~nx0

:: Install .NET SDK
setlocal
set "version=%1"
set "install_dir=%2"
if not defined version set "version=2.2.203"
if not defined install_dir set "install_dir=%LOCALAPPDATA%\Microsoft\dotnet"
echo.
echo %~n0 -Version %version% -InstallDir %install_dir%
call powershell -file "%~dpn0.ps1" -Version %version% -InstallDir %install_dir%
endlocal && set PATH=%install_dir%;%PATH%
