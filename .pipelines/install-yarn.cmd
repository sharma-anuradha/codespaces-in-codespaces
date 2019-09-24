@echo off
:: install-yarn.cmd
echo Running %~nx0

:: Install Yarn
setlocal
set "install_dir=%2"
if not defined install_dir set "install_dir=%SystemDrive%\yarn"
echo.
call npm install yarn@1.17.3 --no-package-lock --no-bin-links --no-save
call robocopy node_modules\yarn %install_dir% /s
endlocal && set PATH=%install_dir%\bin;%PATH%
