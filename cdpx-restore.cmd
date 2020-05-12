@echo off
setlocal

echo Running %~nx0

:: Save working directory so that we can restore it back after building everything. This will make developers happy and then 
:: switch to the folder this script resides in. Don't assume absolute paths because on the build host and on the dev host the locations may be different.
pushd "%~dp0"

:: Install dotnet
set DOTNET_VERSION=3.1.200
call ".pipelines\install-dotnet.cmd" %DOTNET_VERSION%

set DOTNET_ARGS=/m /v:m /p:BuildFrontendBackend=false

:: Dotnet Restore
echo.
echo dotnet restore dirs.proj
call dotnet restore dirs.proj %DOTNET_ARGS%
set EX=%ERRORLEVEL%
if "%EX%" neq "0" (
    popd
    echo Failed to restore correctly.
	exit /b %EX%
)

:: Restore working directory of user so this works fine in dev box.
popd

echo.
echo %~nx0 succeeded

:: Exit with explicit 0 code so that build does not fail.
exit /b 0
