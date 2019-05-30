@echo off
rem echo on
setlocal

rem required
set "service=%1"
set "env=%2"
set "instance=%3"
rem optional
set "urlPath=%4"
set additional_curl_args=
shift
shift
shift
shift
:loop1
    set "arg=%1"
    if "%arg%"=="" goto :done1
    if not "%additional_curl_args%" == "" set "additional_curl_args=%additional_curl_args% "
    set "additional_curl_args=%additional_curl_args%%arg%"
    shift
    goto :loop1
:done1

if "%instance%" == "" (
    echo usage: %0 service env instance [path] [curl-args]
    exit /b 1
)

set "prefix=vsclk"
set "env_name=%prefix%-%service%-%env%"
set "instance_name=%env_name%-%instance%"
set "trafficmanager_name=%instance_name%-tm"
set "trafficmanager_uri=%trafficmanager_name%.trafficmanager.net"
set "cluster_group=%instance_name%-use"
set "cluster_name=%cluster_group%-cluster"

if "%env%" == "dev" set "dnsName=%service%.dev.core.vsengsaas.visualstudio.com"
if "%env%" == "ppe" set "dnsName=%service%-ppe.core.vsengsaas.visualstudio.com"
if "%env%" == "prod" set "dnsName=%service%-rel.core.vsengsaas.visualstudio.com"
if "%env%" == "prod" if "%service%" == "online" set "dnsName=online.visualstudio.com"
if "%dnsName%" == "" (
    echo Invalid env name: %env% >&2
    echo Env must be one of dev, ppe, prod >&2
    exit /b 1
)

call :do_test %dnsName%
call :do_test %trafficmanager_uri%

set "stamps=use usw2 euw asse" 
for %%a in (%stamps%) do ( 
    call :do_test %instance_name%-%%a-tm.trafficmanager.net
)
exit /b

:do_test
echo.
echo ** %1 **
echo.
set "curl_args=-i"
if not "%1" == "%dnsName%" set "curl_args=%curl_args% -k"
if not "%additional_curl_args%" == "" set "curl_args=%curl_args% %additional_curl_args%"
set cmd=curl https://%1/%urlPath% %curl_args% 
echo ^> %cmd%
call %cmd%
echo.

exit /b 0
