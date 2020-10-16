@echo off
setlocal enableDelayedExpansion
set /a "EV2ONLY=0"
if /I "%1" == "ev2only" (
    set /a "EV2ONLY=1"
)

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

::Test-ServiceRollout for Codespaces
set CMD=powershell -f "%~dp0%Scripts\Test-ServiceRollout.ps1" -Component Codespaces
echo call %CMD%
call %CMD%
if errorlevel 1 (
    exit /b %ERRORLEVEL%
)
echo.

if %EV2ONLY% == 1 (
    call :write_host "All validation succeeded" green
    exit /b 0
)

:: Invoke generated validators for all components
set cmd=pwsh -f "%~dp0%Scripts\Invoke-GeneratedValidators.ps1" -All
echo %cmd%
call %cmd%
if errorlevel 1 (
    call :write_host "One or more validators failed" Red
    exit /b 1
)

::HELM Lint Codespaces Charts
for /D %%d in (%~dp0..\bin\debug\ops\Components.generated\Codespaces\charts\*) do (
    call :helm_lint %%d
    if errorlevel 1 (
        exit /b 1
    )
)

call :write_host "All validation succeeded" green
exit /b 0
:: END

::ARM template validation
:az_deployment_group_validate
    setlocal
    set "subscription=%1"
    set "group=%2"
    set "template=%3"
    set "parameters_file=%template:.arm.json=.arm.parameters.json%"
    set "parameters="
    if exist "%parameters_file%" set "parameters=-p @%parameters_file%"
    call :write_host "Validating ARM template '%template%'" DarkBlue
    set cmd=az deployment group validate --no-prompt -f "%template%" %parameters% --subscription %subscription% -g %group% -o none
    echo %cmd%
    call %cmd%
    if errorlevel 1 (
        call :write_host "ARM validation failed" red
        exit /b 1
    )
    call :write_host "ARM validation succeeded" green
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
            call :write_host "helm lint validation failed" red
            exit /b 1
        )
        echo helm template "%chart_dir%" -f "%%v"
        call helm template "%chart_dir%" -f "%%v" > nul
        if errorlevel 1 (
            call :write_Host "helm template validation failed" red
            exit /b 1
        )
    )
    call :write_host "helm validation succeeded" green
    echo.
    exit /b 0

:write_host
    setlocal
    set "message=%1"
    set "color=%2"
    set "foregroundcolor="
    if defined color set "foregroundcolor=-ForegroundColor %color%"
    call pwsh -C Write-Host "%message%" %foregroundcolor%
    exit /b 0

