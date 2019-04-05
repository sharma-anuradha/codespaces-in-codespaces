@if not defined _echo echo off
:: init.cmd

:: get the Git root
for /f "delims=" %%i in ('git rev-parse --show-toplevel') do set "ROOT=%%i"
set "ROOT=%ROOT:/=\%"
for /f "delims=" %%i in ("%ROOT%") do set "ENVIRONMENT_NAME=%%~ni"
echo %ENVIRONMENT_NAME% dev environment

:: Initialize the dev environment
echo.
echo | set /p "dummyName=Initializing Visual Studio dev environment..."
set "dummyName="
rem set "VSDEVCMD_2019_ONLY=1"
call "%~dp0vsdevcmd.cmd"
set "VSDEVCMD_2019_ONLY="
echo (done)

:: Dev tools
echo.
echo %ENVIRONMENT_NAME% tools:
call :which_tool "az             " "https://dotnet.microsoft.com/download/linux-package-manager/ubuntu14-04/sdk-current" 
call :which_tool "code           "
call :which_tool "devenv         "
call :which_tool "docker         "
call :which_tool "docker-compose "
call :which_tool "dotnet         " "https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-apt?view=azure-cli-latest"
call :which_tool "git            " "https://git-scm.com/download/linux"
call :which_tool "helm           " "https://github.com/helm/helm/blob/master/docs/install.md"
call :which_tool "istioctl       " "https://istio.io/docs/setup/kubernetes/quick-start/"
call :which_tool "kubectl        " "https://kubernetes.io/docs/tasks/tools/install-kubectl/#install-kubectl-binary-using-curl"
call :which_tool "msbuild        "

:: Macros
doskey /macrofile="%~dp0aliases.macros"
echo.
echo %ENVIRONMENT_NAME% macros:
for /f "delims== tokens=1,2" %%i in ('doskey /macros') do call :echo_key_value %%i %%j

:: Done
exit /b 0

:: Subroutines

:echo_key_value
    setlocal
    set "key=%1"
    shift
    set "value=%1 %2 %3 %4 %5 %6 %7 %8 %9"
    set "line=%key%                       "
    echo %line:~0,15%: %value%
    endlocal
    exit /b 0

:echo_no_quotes
    setlocal
    set message=%1
    echo %message:"=%
    endlocal
    exit /b 0

:set_var
    set %1=%2
    exit /b %errorlevel%

:where_tool
    set temp_file=%temp%\init_where_tool.tmp
    if exist "%temp_file%" del "%temp_file%"
    set tool_path=
    call where %tool% > "%temp_file%" 2> nul
    if errorlevel 0 (
        set /p tool_path=< "%temp_file%"
    )
    if exist "%temp_file%" del "%temp_file%"
    exit /b %errorlevel%

:which_tool
    setlocal
    set "tool=%1"
    set "tool=%tool:"=%"
    set "install=%2"
    call :where_tool
    if not defined tool_path (
        if defined install (
            call :echo_no_quotes "%tool%: not found on path, see %install:"=%"
        ) else (
            call :echo_no_quotes "%tool%: not found on path"
        )
        exit /b 1
    ) else (
        call :echo_no_quotes "%tool%: %tool_path%"
    )
    exit /b 0
