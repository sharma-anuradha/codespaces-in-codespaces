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

::Test-ServiceRollout for Stub
set CMD=powershell -f "%~dp0%Scripts\Test-ServiceRollout.ps1" -Component stub
echo call %CMD%
call %CMD%
if errorlevel 1 (
    exit /b %ERRORLEVEL%
)
echo.

::Test-ServiceRollout for Core
set CMD=powershell -f "%~dp0%Scripts\Test-ServiceRollout.ps1" -Component core
echo call %CMD%
call %CMD%
if errorlevel 1 (
    exit /b %ERRORLEVEL%
)
echo.

::ARM templates for dev environment
for %%f in (%~dp0..\bin\debug\ops\Components.generated\Core\core.dev-ctl\arm\*.arm.json) do (
    call :az_deployment_group_validate vscs-core-dev-ctl vscs-core-dev %%f
    if errorlevel 1 (
        exit /b 1
    )
)

::ARM templates for dev instances
for %%f in (%~dp0..\bin\debug\ops\Components.generated\Core\core.dev-ctl-ci\arm\*.arm.json) do (
    call :az_deployment_group_validate vscs-core-dev-ctl vscs-core-dev-ci %%f
    if errorlevel 1 (
        exit /b 1
    )
)

::ARM templates for dev regions
for %%f in (%~dp0..\bin\debug\ops\Components.generated\Core\Core.dev-ctl-ci-us-w2\arm\*.arm.json) do (
    call :az_deployment_group_validate vscs-core-dev-ctl vscs-core-dev-ci-us-w2 %%f
    if errorlevel 1 (
        exit /b 1
    )
)

::HELM Lint Charts
for /D %%d in (%~dp0..\bin\debug\ops\Components.generated\Core\charts\*) do (
    call :helm_lint %%d
    if errorlevel 1 (
        exit /b 1
    )
)

exit /b 0
:: END

::ARM template validation
:az_deployment_group_validate
    setlocal
    set "subscription=%1"
    set "group=%2"
    set "template=%3"
    echo Validating ARM tempalte "%template%"
    set cmd=az deployment group validate -f "%template%" --subscription %subscription% -g %group% -o none
    echo %cmd%
    call %cmd%
    if errorlevel 1 (
        call pwsh -C Write-Host "validation failed" -ForegroundColor red
        exit /b 1
    )
    call pwsh -C Write-Host "validation succeeded" -ForegroundColor green
    echo.
    exit /b 0

:helm_lint
    setlocal
    set "chart_dir=%1"
    set "chart_name=%~n1"
    echo Validating Helm chart "%chart_name%"
    for /R %chart_dir%\..\..\ %%v in (*%chart_name%.values.yaml) do (
        echo helm lint "%chart_dir%" -f "%%v"
        call helm lint "%chart_dir%" -f "%%v"
        if errorlevel 1 (
            call pwsh -C Write-Host "validation failed" -ForegroundColor red
            exit /b 1
        )
    )
    call pwsh -C Write-Host "validation succeeded" -ForegroundColor green
    echo.
    exit /b 0
