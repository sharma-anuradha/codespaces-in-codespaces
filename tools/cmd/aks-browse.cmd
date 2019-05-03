@echo off

:main
    setlocal
    set "stamp=%1"
    set "service_instance=%2"
    set "subscription=%3"
    if "%stamp%" equ "" set "stamp=usw2"
    if "%service_instance%" equ "" set "service_instance=vsclk-online-dev-ci"
    if "%subscription%" equ "" set "subscription=vsclk-core-dev"
    set "cluster_group=%service_instance%-%stamp%"
    set "cluster_name=%cluster_group%-cluster"

    echo az aks browse -g %cluster_group% --name %cluster_name% --subscription %subscription%
    call az aks browse -g %cluster_group% --name %cluster_name% --subscription %subscription%
    exit /b
