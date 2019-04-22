@echo off

:main
    setlocal
    call :helm_delete_releases use
    call :helm_delete_releases usw2
    call :helm_delete_releases euw
    call :helm_delete_releases asse
    exit /b 0

:helm_delete_releases
    setlocal
    set stamp=%1
    call :set_context %stamp%
    call :helm_delete vsclk-core-svc-portal
    call :helm_delete vsclk-core-svc-envreg
    exit /b 0

:helm_delete
    setlocal
    set release=%1
    echo helm delete %1
    call helm delete %1 --purge --tiller-namespace default
    exit /b 0

:set_context
    setlocal
    set stamp=%1
    set rg=vsclk-core-svc-dev-ci-%stamp%
    set cluster=%rg%-cluster
    call az aks get-credentials -g %rg% -n %cluster%
    call kubectl config current-context
    exit /b 0
