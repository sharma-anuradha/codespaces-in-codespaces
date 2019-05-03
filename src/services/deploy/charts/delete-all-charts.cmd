@echo off

:main
    setlocal
    set "service_instance=%1"
    set "subscription=%2"
    if "%service_instance%" equ "" set "service_instance=vsclk-online-dev-ci"
    if "%subscription%" equ "" set "subscription=vsclk-core-dev"
    call :helm_delete_releases use
    call :helm_delete_releases usw2
    call :helm_delete_releases euw
    call :helm_delete_releases asse
    exit /b 0

:helm_delete_releases
    setlocal
    set "stamp=%1"
    call :set_context %stamp%
    rem the helm release names are defined in the release pipeline using short names
    call :helm_delete portal
    call :helm_delete envreg
    call :helm_delete signalr
    exit /b 0

:helm_delete
    setlocal
    set "release=%1"
    echo helm delete %release% --purge --tiller-namespace default
    call helm delete %release% --purge --tiller-namespace default
    exit /b 0

:set_context
    setlocal
    set "stamp=%1"
    set "cluster_group=%service_instance%-%stamp%"
    set "cluster_name=%rg%-cluster"
    call az aks get-credentials -g %cluster_group% -n %cluster_name% --subscription %subscription%
    call kubectl config current-context
    exit /b 0
