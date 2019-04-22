@echo off

:main
    setlocal
    call :validate vsclk-envreg-webapi
    exit /b 0

:validate
    setlocal
    set "chartDirectory=%1"

    if not exist "%1" (
        echo Chart directory does not exist: %chartDirectory%
        exit /b 1
    )

    echo Bootstrapping secrets for %chartDirectory%
    call helm azure-secrets -bootstrap -chartDirectory %chartDirectory%

    echo Generating templates for %chartDirectory%
    set "template_dir=%temp%\helm"
    if not exist "%template_dir%" mkdir "%template_dir%"
    call helm template %chartDirectory% --notes --tiller-namespace default --name validate --output-dir %template_dir% --set image.tag=latest --set image.repositoryUrl=validate.azurecr.io

    for %%f in (%template_dir%\%chartDirectory%\templates\*.yaml) do (
        echo Applying "%%f"
        call kubectl apply -f "%%f" --dry-run
    )
    exit /b 0

