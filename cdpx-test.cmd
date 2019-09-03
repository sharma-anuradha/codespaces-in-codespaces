@echo off
setlocal

echo Running %~nx0

:: Save working directory so that we can restore it back after building everything. This will make developers happy and then 
:: switch to the folder this script resides in. Don't assume absolute paths because on the build host and on the dev host the locations may be different.
pushd "%~dp0"

:: Add dotnet to PATH. (It should have been already installed by the build task.)
set DOTNET_VERSION=2.2.401
call ".pipelines\install-dotnet.cmd" %DOTNET_VERSION%

set DOTNET_ARGS=/v:m /p:RestorePackages=false

:: Dotnet Test
echo.
echo dotnet test dirs.proj
call dotnet test --no-build --configuration Release --filter "Category!=IntegrationTest" -p:CodeCoverage=true dirs.proj %DOTNET_ARGS%
set EX=%ERRORLEVEL%

if "%EX%" neq "0" (
    echo "Tests failed. Check the Tests tab of the build for results."
)

:: Restore working directory of user so this works fine in dev box.
popd

exit /b %EX%
