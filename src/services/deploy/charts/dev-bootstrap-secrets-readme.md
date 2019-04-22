# Bootstrap Secrets on a DEV box

Bootstraps secrets for the given chart using the [Helm azure-secrets plugin](https://dev.azure.com/devdiv/devdiv/_wiki/wikis/DevDiv.wiki?wikiVersion=GBwikiMaster&pagePath=%2FEngineering%20System%20%26%20Tools%2FVSEng%20SaaS%2FOnboarding%20Tools%2FAzure%20Secrets%20Helm%20Plugin&pageId=957).

See the 'azure-secrets' section in values.yaml for static config for dev.

**WARNING: This plugin works on Windows only. PowerShell is not your friend, Linux!**

## Run azure-secrets on a chart

```cmd
set env=dev
set chart_directory=vsclk-envreg-webapi
set chart_directory=vsclk-envreg-portal
call helm azure-secrets -bootstrap -env %env% -chartDirectory %chart_directory%
```

## Install helm-azure-secrets plugin

```cmd
REM Install
md %userprofile%\.helm\plugins
helm plugin install \\ddfiles\vseng\Tools\helm-azure-secrets

REM Verify
helm azure-secrets
```


