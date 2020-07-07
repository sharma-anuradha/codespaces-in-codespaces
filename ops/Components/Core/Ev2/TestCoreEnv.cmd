@echo off
setlocal

call 

set UNIQUE=%date:~4,10%-%time::=-%
set UNIQUE=%UNIQUE:/=-%
set UNIQUE=%UNIQUE:.=-%
set UNIQUE=%UNIQUE: =%

set SUB=vscs-core-test
set LOCATION=westus2
set ENV=Temp

set NAME=CoreOpsEnvSub
set DEPLOYMENT=%NAME%-%UNIQUE%
set TEMPLATE=%NAME%.Template.jsonc
set PARAMETERS=%NAME%.%ENV%.Parameters.jsonc

call :invoke az deployment sub validate ^
  --no-prompt ^
  --name "%DEPLOYMENT%" ^
  --location "%LOCATION%" ^
  --template-file ".\ARM\%TEMPLATE%" ^
  --parameters @".\ARM\%PARAMETERS%" ^
  --subscription "%SUB%"
if errorlevel 1 exit /b %errorlevel%
echo.

call :invoke az deployment sub create ^
  --no-prompt ^
  --name "%DEPLOYMENT%" ^
  --location "%LOCATION%" ^
  --template-file ".\ARM\%TEMPLATE%" ^
  --parameters @".\ARM\%PARAMETERS%" ^
  --subscription "%SUB%"
if errorlevel 1 exit /b %errorlevel%
echo.

set NAME=CoreOpsEnv
set DEPLOYMENT=%NAME%-%UNIQUE%
set TEMPLATE=%NAME%.Template.jsonc
set PARAMETERS=%NAME%.%ENV%.Parameters.jsonc
set GROUPNAME=vscs-core-test-ops

call :invoke az deployment group validate ^
  --no-prompt ^
  --name "%DEPLOYMENT%" ^
  --resource-group "%GROUPNAME%" ^
  --template-file ".\ARM\%TEMPLATE%" ^
  --parameters @".\ARM\%PARAMETERS%" ^
  --subscription "%SUB%"
if errorlevel 1 exit /b %errorlevel%
echo.

call :invoke az deployment group create ^
  --no-prompt ^
  --name "%DEPLOYMENT%" ^
  --resource-group "%GROUPNAME%" ^
  --template-file ".\ARM\%TEMPLATE%" ^
  --parameters @".\ARM\%PARAMETERS%" ^
  --subscription "%SUB%"
if errorlevel 1 exit /b %errorlevel%
echo.

exit /b 0

:invoke
    echo %*
    call %*
    exit /b %errorlevel%
