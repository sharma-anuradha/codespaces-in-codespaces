@echo off
setlocal

set "prefix=vsclk"
set "service=core-svc"
set "env=dev"
set "instance=ci"
set "env_name=%prefix%-%service%-%env%"

set "stamp=use"
REM set "stamp=usw2"
REM set "stamp=euw"
REM set "stamp=asse"

set "instance_name=%env_name%-%instance%"
set "cluster_group=%instance_name%-%stamp%"
set "cluster_name=%cluster_group%-cluster"

call az aks browse -g %cluster_group% --name %cluster_name%
exit /b

