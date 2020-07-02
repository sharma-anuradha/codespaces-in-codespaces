# replaceNugetConfig.ps1

param (
    [switch]$test
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'

$PAT=$env:CDP_DEFAULT_CLIENT_PACKAGE_PAT

if ($PAT -ne $null)
{
    Write-Host "Detected CDPX environment. Injecting credentials into nuget.config file."
}
elseif ($test)
{
    Write-Host "Not running in CDPX. Updating nuget.config without PAT."
}
else
{
    Write-Host "Not running in CDPX. No need to update credentials in nuget config."
    exit 0
}

function AddSource {
    param(
        [string]$name,
        [string]$source,
        [string]$pat
    )

    if (!$pat)
    {
        $pat="*"
    }

    &nuget sources add -Name "$name" -Source "$source" -UserName "PAT" -Password "$pat" -Verbosity detailed -NonInteractive -ConfigFile "$nugetConfig" | Out-Default

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to add source $name from $source to $nugetConfig"
        exit $?
    }
}

$nugetConfig = Resolve-Path "$PSScriptRoot\..\NuGet.config"

# Read the sources from the original nuget.config
[xml]$nugetConfigXml = Get-Content $nugetConfig
$packageSources = $nugetConfigXml.configuration.packageSources.add
$sources = $packageSources | % { @{ name=$_.key; source=$_.value } }

$config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
"@

Write-Host "Deleting $nugetConfig if it exists"
Remove-Item -Force -Path $nugetConfig -ErrorAction Ignore

Write-Host "Generating root NuGet.config at $nugetConfig"
$config | Out-File -FilePath $nugetConfig -Encoding utf8

$sources | % { AddSource -name $_.name -source $_.source -pat $PAT }

$nugetConfig = Resolve-Path "$PSScriptRoot\..\src\AzurePortal\src\NuGet.config"

# Read the sources from the original nuget.config
[xml]$nugetConfigXml = Get-Content $nugetConfig
$packageSources = $nugetConfigXml.configuration.packageSources.add
$sources = $packageSources | % { @{ name=$_.key; source=$_.value } }

$config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
"@

Write-Host "Deleting $nugetConfig if it exists"
Remove-Item -Force -Path $nugetConfig -ErrorAction Ignore

Write-Host "Generating root NuGet.config at $nugetConfig"
$config | Out-File -FilePath $nugetConfig -Encoding utf8

$sources | % { AddSource -name $_.name -source $_.source -pat $PAT }
