@echo off
setlocal

:: build-dotnet.cmd
echo Building %~nx0

:: Save working directory so that we can restore it back after building everything. This will make developers happy and then 
:: switch to the folder this script resides in. Don't assume absolute paths because on the build host and on the dev host the locations may be different.
pushd "%~dp0"

:: Install dotnet
set DOTNET_VERSION=2.2.203
call ".pipelines\install-dotnet.cmd" %DOTNET_VERSION%

:: Install node
set NODE_VERSION=v10.15.3
call ".pipelines\install-node.cmd" %NODE_VERSION%

call node --version

set DOTNET_ARGS=/m /v:m /p:RestorePackages=false

:: Dotnet Build
echo.
echo dotnet build dirs.proj
call dotnet build --no-restore --configuration Release dirs.proj %DOTNET_ARGS%
set EX=%ERRORLEVEL%
if "%EX%" neq "0" (
    popd
    echo "Failed to build correctly."
	exit /b %EX%
)

:: Dotnet Publish
echo.
echo dotnet publish dirs.proj
call dotnet publish --no-restore --configuration Release --no-build dirs.proj %DOTNET_ARGS%
set EX=%ERRORLEVEL%
if "%EX%" neq "0" (
    popd
    echo "Failed to publish correctly."
	exit /b %EX%
)

:: Restore working directory of user so this works fine in dev box.
popd

echo.
echo %~nx0 succeeded

:: Exit with explicit 0 code so that build does not fail.
exit /b 0
