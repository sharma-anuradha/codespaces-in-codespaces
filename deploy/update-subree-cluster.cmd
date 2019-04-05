@if not defined _echo echo off
setlocal

:: Subtree Context
set "PREFIX=deploy/cluster"
set "REMOTE=vsclk-cluster"
set "REMOTE_URL=https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-cluster"

:: Subtree commands must run from the git root
for /f "delims=" %%i in ('git rev-parse --show-toplevel') do set "ROOT=%%i"
set ROOT=%ROOT:/=\%
pushd %ROOT%

:: Fixup remote and disable pushing
echo setting up git remote %PREFIX%
git remote remove %REMOTE% 2>nul >nul
git remote add %REMOTE% %REMOTE_URL% >nul
git remote set-url --push %REMOTE% dont-push-to-%REMOTE% >nul

:: Add or pull the subtree
if not exist "%ROOT%\%PREFIX:/=\%" (
    echo adding subtree %PREFIX%
    git subtree add --prefix=%PREFIX% %REMOTE% master --message "add subtree '%PREFIX%' from %REMOTE% master" --squash
) else (
    echo pulling subtree %PREFIX%
    git subtree pull --prefix=%PREFIX% %REMOTE% master --message "pull subtree '%PREFIX%' from %REMOTE% master" --squash
)

popd

exit /b 0
