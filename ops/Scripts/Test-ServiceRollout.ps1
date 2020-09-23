# Test-ServiceRollout.ps1
# Test one of the service rollouts from the Components.generated folder.

#requires -version 5.1

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Component,
    [string]$RolloutSpecPattern = "*.rolloutspec.json?",
    [string]$ComponentsGeneratedFolder,
    [switch]$Online
)

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'

# Global error handling
trap {
    Write-Error $_ -ErrorAction "Continue"
    exit 1
}

if ($PSVersionTable.PSVersion.Major -gt 5) {
    throw "The Ev2 cmdlets require PowerShell 5.1 with full .NET Framework :("
}

function Get-ServiceRoot() {
    $path = [System.IO.Path]::GetFullPath(([System.IO.Path]::Combine($ComponentsGeneratedFolder, $Component)))
    $serviceRoot = Get-Item -Path $path
    if (!$serviceRoot) {
        throw "Service root does not exist: $path"
    }
    $serviceRoot
}

function Get-RolloutSpecs() {
    $serviceRoot = Get-ServiceRoot
    $specs = $serviceRoot.GetFiles($RolloutSpecPattern, [System.IO.SearchOption]::AllDirectories) | % { $_.FullName }

    if (!$specs) {
        throw "Rollout spec(s) not found: '$serviceRoot\$RolloutSpecPattern'"
    }

    $specs
}

function Import-AzureDeploymentExpressClient {
    $moduleName = "Microsoft.Azure.Deployment.Express.Client"
    try {
        $azureServiceDeployClient = Get-Item -Path ([System.IO.Path]::Combine($env:LOCALAPPDATA, "Microsoft", "AzureServiceDeployClient"))
        $latestVersion = $azureServiceDeployClient.GetDirectories() | % { New-Object -TypeName System.Version -ArgumentList $_.Name } | Sort-Object -Descending | Select-Object -First 1 | % { $_.ToString() }
        $moduleDll = (Get-Item -Path ([System.IO.Path]::Combine($azureServiceDeployClient.FullName, $latestVersion, "$moduleName.dll"))).FullName
        Import-Module -Global $moduleDll
    }
    catch {
        "Could not load $modulName" | Write-Host -ForegroundColor Red
        "Install the Ev2 powershell cmdlets from https://ev2docs.azure.net/references/cmdlets/Intro.html" | Write-Host -ForegroundColor Red
        throw
    }
}

if (!$ComponentsGeneratedFolder) {
    $ComponentsGeneratedFolder = "$PSScriptRoot\..\..\bin\debug\ops\Components.generated"
}

$ComponentsGeneratedFolder | Write-Host -ForegroundColor Yellow
$ComponentsGeneratedFolder = [System.IO.Path]::GetFullPath($ComponentsGeneratedFolder)
$ComponentsGeneratedFolder | Write-Host -ForegroundColor Yellow
if (!(Test-Path -Path $ComponentsGeneratedFolder -PathType Container)) {
    throw "Generated folder does not exist: '$ComponentsGeneratedFolder'. Please generate outputs and build ops.csproj."
}

Import-AzureDeploymentExpressClient
$serviceGroupRoot = (Get-ServiceRoot).FullName
$rolloutSpecs = Get-RolloutSpecs
"Validating rollout specs:" | Write-Host -ForegroundColor DarkGray
$rolloutSpecs | Out-String | Write-Host -ForegroundColor DarkGray

$goodRolloutSpecs = @()
$badRolloutSpecs = @()

if ($Online) {
    $rolloutSpecs = $rolloutSpecs | Where-Object { $_.Contains(".dev-") }
}

$rolloutSpecs | ForEach-Object {
    $rolloutSpec = $_
    try {
        if ($Online) {
            "Validating $rolloutSpec" | Write-Host -ForegroundColor DarkBlue
            Test-AzureServiceRollout -ServiceGroupRoot $serviceGroupRoot -RolloutSpec $rolloutSpec -EnableStrictValidation -TreatWarningAsError -RolloutInfra 'Test' -CreateResourceGroupIfRequired $true -WaitToComplete
            $goodRolloutSpecs += $rolloutSpec
        }
        else {
            "Validating $rolloutSpec" | Write-Host -ForegroundColor DarkBlue
            Test-AzureServiceRollout -ServiceGroupRoot $serviceGroupRoot -RolloutSpec $rolloutSpec -ClientMode -EnableStrictValidation -TreatWarningAsError
            $goodRolloutSpecs += $rolloutSpec
        }
    }
    catch {
        $badRolloutSpecs += $rolloutSpec
    }
}

if ($goodRolloutSpecs) {
    "Valid rolloutspecs:" | Write-Host -ForegroundColor Green
    $goodRolloutSpecs | Out-String | Write-Host -ForegroundColor Green
}

if ($badRolloutSpecs) {
    "Invalid rolloutspecs:" | Write-Host -ForegroundColor Yellow
    $badRolloutSpecs | Out-String | Write-Host -ForegroundColor Yellow
    throw "validation failed"
}

"validation succeeded" | Write-Host -ForegroundColor Green
