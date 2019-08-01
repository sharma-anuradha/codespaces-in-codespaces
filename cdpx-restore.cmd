@echo off
setlocal

echo Running %~nx0

:: Save working directory so that we can restore it back after building everything. This will make developers happy and then 
:: switch to the folder this script resides in. Don't assume absolute paths because on the build host and on the dev host the locations may be different.
pushd "%~dp0"

:: Install dotnet
set DOTNET_VERSION=2.2.401
call ".pipelines\install-dotnet.cmd" %DOTNET_VERSION%

:: Install node
set NODE_VERSION=v10.15.3
call ".pipelines\install-node.cmd" %NODE_VERSION%

set DOTNET_ARGS=/m /v:m

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

:: NPM install and build
:: We need to copy sources to a tmp directory, call install and build, and then copy the built binaries back to our ClientApp directory
echo.
echo npm install and build
pushd ".\src\services\containers\VsClk.Portal.WebSite\ClientApp"
call robocopy . %tmp%\portalspabuild\ /s 
pushd %tmp%\portalspabuild\
echo.
echo npm-install-project
call npm install
set EX=%ERRORLEVEL%
if "%EX%" neq "0" (
    echo Failed to npm-install-project correctly.
    echo .npmrc:
    type ".npmrc"
    echo %userprofile%\.npmrc:
    type "%userprofile%\.npmrc"
    popd
	exit /b %EX%
)
echo.
echo npm-build-project
call node_modules\.bin\react-scripts build
set EX=%ERRORLEVEL%
if "%EX%" neq "0" (
    popd
    echo Failed to npm-build-project correctly.
	exit /b %EX%
)
popd

echo.
echo copy output files
call robocopy %tmp%\portalspabuild\build\ .\build /s
popd

:: Restore working directory of user so this works fine in dev box.
popd

echo.
echo %~nx0 succeeded

:: Exit with explicit 0 code so that build does not fail.
exit /b 0
