@echo off
rem echo on
setlocal

set "prefix=vsclk"
set "service=core-svc"
set "env=dev"
set "instance=ci"
set "env_name=%prefix%-%service%-%env%"
set "instance_name=%env_name%-%instance%"
set "trafficmanager_name=%instance_name%-tm"
set "trafficmanager_uri=%trafficmanager_name%.trafficmanager.net"
set "cluster_group=%instance_name%-use"
set "cluster_name=%cluster_group%-cluster"
set "dnsName=ci.dev.core.vsengsaas.visualstudio.com"


call :do_test %dnsName%
call :wait

call :do_test %trafficmanager_uri%
call :wait

set "stamps=use usw2 euw asse" 
for %%a in (%stamps%) do ( 
    call :do_test %instance_name%-%%a-tm.trafficmanager.net
    call :wait
)
exit /b

:do_test
echo.
echo.
echo ** %1 **
echo.
echo curl http://%1/healthz
call curl -i "http://%1/healthz"
echo.
echo curl https://%1 --header "Host: %dnsName%"
call curl -i -k "https://%1" --header "Host: %dnsName%"
echo.
exit /b 0

:wait
set /p "DUMMY=Hit ENTER to continue..."
