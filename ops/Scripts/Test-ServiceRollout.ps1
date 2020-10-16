# Test-ServiceRollout.ps1
# Test one of the service rollouts from the Components.generated folder.

#requires -version 5.1

[CmdletBinding(DefaultParameterSetName = 'Test')]
param(
    [Parameter(Mandatory = $true)]
    [string]$Component,
    [string]$ComponentsGeneratedFolder = $null,
    [Parameter(ParameterSetName = 'Test')]
    [string]$RolloutSpecPattern = '*.rolloutspec.json?',
    [Parameter(ParameterSetName = 'Test')]
    [switch]$Online,
    [Parameter(ParameterSetName = 'Deploy')]
    [switch]$Deploy,
    [Parameter(Mandatory = $true, ParameterSetName = 'Deploy')]
    [string]$RolloutSpecPath
)

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'

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

    if ($Deploy) {
        $path = [System.IO.Path]::GetFullPath(([System.IO.Path]::Combine($ComponentsGeneratedFolder, $Component, $RolloutSpecPath)))
        if (!(Test-Path -Path $path -PathType Leaf)) {
            throw "Rollout spec(s) not found: '$path'"
        }
        $specs = @( $path )
    }
    else {
        $specs = $serviceRoot.GetFiles($RolloutSpecPattern, [System.IO.SearchOption]::AllDirectories) | % { $_.FullName }
        if (!$specs) {
            throw "Rollout spec(s) not found: '$serviceRoot\$RolloutSpecPattern'"
        }
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

function Test-RolloutSpecs() {
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
                Test-AzureServiceRollout -ServiceGroupRoot $script:serviceGroupRoot -RolloutSpec $rolloutSpec -EnableStrictValidation -TreatWarningAsError -RolloutInfra 'Test' -CreateResourceGroupIfRequired $true -WaitToComplete
                $goodRolloutSpecs += $rolloutSpec
            }
            else {
                "Validating $rolloutSpec" | Write-Host -ForegroundColor DarkBlue
                Test-AzureServiceRollout -ServiceGroupRoot $script:serviceGroupRoot -RolloutSpec $rolloutSpec -ClientMode -EnableStrictValidation -TreatWarningAsError
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
}

function Deploy-RolloutSpecs() {
    $goodRolloutSpecs = @()
    $badRolloutSpecs = @()

    $rolloutSpecs | ForEach-Object {
        $rolloutSpec = $_
        try {
            "Deploying $rolloutSpec" | Write-Host -ForegroundColor DarkBlue
            $rollout = New-AzureServiceRollout -ServiceGroupRoot $script:serviceGroupRoot -RolloutSpec $rolloutSpec -RolloutInfra 'Test' -EnableStrictValidation -WaitToComplete
            $rolloutInfo = $rollout | ConvertTo-Json -Depth 20 | Out-String
            $rolloutInfo = $rolloutInfo.Replace('    ', ' ')
            $rolloutInfo | Write-Host -ForegroundColor DarkGray
            if ($rollout.Status -eq "Failed") {
                $badRolloutSpecs += $rolloutSpec
            } else {
                $goodRolloutSpecs += $rolloutSpec
            }
        }
        catch {
            Write-Error $_ -ErrorAction "Continue"
            $badRolloutSpecs += $rolloutSpec
        }
    }

    if ($goodRolloutSpecs) {
        "Good deployments:" | Write-Host -ForegroundColor Green
        $goodRolloutSpecs | Out-String | Write-Host -ForegroundColor Green
    }

    if ($badRolloutSpecs) {
        "Failed deployments:" | Write-Host -ForegroundColor Yellow
        $badRolloutSpecs | Out-String | Write-Host -ForegroundColor Yellow
        throw "deployment failed"
    }

    "deployment succeeded" | Write-Host -ForegroundColor Green
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
$script:serviceGroupRoot = (Get-ServiceRoot).FullName
$script:rolloutSpecs = Get-RolloutSpecs
"Service group root: $($script:serviceGroupRoot)" | Write-Host -ForegroundColor DarkGray
"Rollout specs: [" | Write-Host -ForegroundColor DarkGray
$script:rolloutSpecs | Out-String | Write-Host -ForegroundColor DarkGray
"]" | Write-Host -ForegroundColor DarkGray

if ($Deploy) {
    Deploy-RolloutSpecs
}
else {
    Test-RolloutSpecs
}
