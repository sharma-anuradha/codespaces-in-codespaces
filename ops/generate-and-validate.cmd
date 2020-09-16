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
echo.

::ARM templates
for %%f in (%~dp0..\bin\debug\ops\Components.generated\Core\Ev2\ARM\Core.dev*.json) do (
    call :az_deployment_group_validate %%f
    REM echo call az deployment group validate -f %%f --subscription vscs-core-dev-ctl -g vscs-core-dev-ci-us-w2 -o none
    REM      call az deployment group validate -f %%f --subscription vscs-core-dev-ctl -g vscs-core-dev-ci-us-w2 -o none
    if errorlevel 1 (
        exit /b %ERRORLEVEL%
    )
    REM call pwsh -C Write-Host "validation succeeded" -ForegroundColor green
)

exit /b 0
:: END


::ARM template validation
:az_deployment_group_validate
    setlocal
    set "template=%1"
    echo Validating %template%
    :: short-cut: use the same subscription and group for all validation
    set cmd=az deployment group validate -f "%template%" --subscription vscs-core-dev-ctl -g vscs-core-dev-ci-us-w2 -o none
    echo %cmd%
    call %cmd%
    if errorlevel 1 (
        call pwsh -C Write-Host "validation failed" -ForegroundColor red
        exit /b %ERRORLEVEL%
    )
    call pwsh -C Write-Host "validation succeeded" -ForegroundColor green
    echo.
    exit /b 0
