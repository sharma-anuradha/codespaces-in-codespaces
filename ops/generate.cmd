@echo off
setlocal

::Update-GeneratedFiles
set CMD=pwsh -f "%~dp0%Scripts\Update-GeneratedFiles.ps1" -UpdateComponents
echo call %CMD%
call %CMD%
if errorlevel 1 (
    exit /b %ERRORLEVEL%
)

::build
set CMD=dotnet build /m "%~dp0%ops.csproj"
echo call %CMD%
call %CMD%
if errorlevel 1 (
    exit /b %ERRORLEVEL%
)

::Test-ServiceRollout
set CMD=powershell -f "%~dp0%Scripts\Test-ServiceRollout.ps1" -Component core
echo call %CMD%
call %CMD%
if errorlevel 1 (
    exit /b %ERRORLEVEL%
)

exit /b 0
