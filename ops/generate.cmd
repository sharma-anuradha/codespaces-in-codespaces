@echo off
setlocal

::Run from generator dir
set ROOT=%~dp0
set GENERATOR=%ROOT%Generator\

pushd "%GENERATOR%"
set INPUT=..\Components\
set OUTPUT=..\Components.generated\

::Install
call npm install > NUL
if errorlevel 1 (
    echo npm install failed
    popd
    exit 1
)

::Clean
if exist "%OUTPUT%" (
    echo deleting "%OUTPUT%"
    rd /s /q "%OUTPUT%"
)

::Generate
echo generating "%OUTPUT%"
set CMD=ts-node-script index.ts %INPUT% %OUTPUT%
echo %CMD%
call %CMD%

popd