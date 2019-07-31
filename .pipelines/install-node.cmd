@echo off
:: install-node.cmd
echo Running %~nx0

:: Install NodeJS
setlocal
set "version=%1"
set "install_dir=%2"
set "x64hash=%3"
set "x86hash=%4"
if not defined version set "version=v10.15.3"
if not defined install_dir set "install_dir=%SystemDrive%\n"
if not defined x64hash set "x64hash=93c881fdc0455a932dd5b506a7a03df27d9fe36155c1d3f351ebfa4e20bf1c0d"
if not defined x86hash set "x86hash=fc28bbd08b3d9b621c7c0ecd2b42506ca2f356f31f2b64210f413b34cff31799"
echo.
echo %~n0 -Version %version% -InstallDir %install_dir% -X64Hash %x64hash% -X86Hash %x86hash%
call powershell -file "%~dpn0.ps1" -Version %version% -InstallDir %install_dir% -X64Hash %x64hash% -X86Hash %x86hash%
endlocal && set PATH=%install_dir%;%PATH%
