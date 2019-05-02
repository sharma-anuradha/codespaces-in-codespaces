@echo off

:main
    setlocal
    set "chart_root=%~dp0"
    set "common_dir=%chart_root%common"
    set "prefix=vsclk"
    set "name=%1"
    if "%name%" equ "" set "name=online"
    set "env=%2"
    if "%env%" equ "" set "env=dev"
    set "instance=%3"
    if "%instance%" equ "" set "instance=ci"
    set "instance_name=%prefix%-%name%-%env%-%instance%"
    set "values_file=%common_dir%\%instance_name%.values.yaml"
    if not exist "%values_file%" (
        echo Values override file does not exist: %values_file%
        exit /b 1
    )
    for /d %%d in ("vsclk-*") do call :validate %%d
    exit /b 0

:validate
    setlocal
    set "chart_name=%1"
    if "%chart_name%" equ "" (
        echo Chart name is required
        exit /b 1
    )
    set "chart_dir=%chart_root%%chart_name%"
    if not exist "%chart_dir%" (
        echo Chart directory does not exist: %chart_dir%
        exit /b 1
    )

    echo Bootstrapping secrets for %chart_dir% with environment %env%
    echo helm azure-secrets -bootstrap -chartDirectory "%chart_dir%" -env "%env%"
    call helm azure-secrets -bootstrap -chartDirectory "%chart_dir%" -env "%env%"
    set "err=%ERRORLEVEL%"
    if not %err% equ 0 (
        echo helm azure-secrets failed
        exit /b 1
    )

    echo Generating templates for %chart_dir% into %template_dir%
    set "template_dir=%temp%\helm\%env%"
    if not exist "%template_dir%" mkdir "%template_dir%"
    echo helm template %chart_dir% --notes --tiller-namespace default --name validate --output-dir %template_dir% --set image.tag=latest -f "%values_file%"
    call helm template %chart_dir% --notes --tiller-namespace default --name validate --output-dir %template_dir% --set image.tag=latest -f "%values_file%"
    set "err=%ERRORLEVEL%"
    if not %err% equ 0 (
        echo helm template failed
        exit /b 1
    )
    call dir /s /b "%template_dir%\%chart_name%\templates"
    for %%f in (%template_dir%\%chart_dir%\templates\*.yaml) do (
        echo Applying "%%f"
        call kubectl apply -f "%%f" --dry-run
    )
    exit /b 0
