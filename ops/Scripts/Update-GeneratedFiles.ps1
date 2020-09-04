# Onboard-LegacySubscription.ps1

#requires -version 7.0

[CmdletBinding()]
param(
    [switch]$UpdateAll,
    [switch]$UpdateSubscriptions,
    [string]$ExcelFile,
    [switch]$UpdateAppSettings,
    [switch]$UpdateComponents
)

# Global error handling -- ensure non-zero exit code on failure
trap {
    Write-Error $_
    exit 1
}

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:Verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

# Import the subscripton tracker
. "$PSScriptRoot\Subscription-Tracker.ps1"

function Get-ComponentsFolder {
    $folder = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\Components")
    Write-Verbose "ComponentsFolder: $folder" -Verbose:$Verbose
    $folder
}

function Get-ComponentsGeneratedFolder {
    $folder = "$(Get-ComponentsFolder).generated"
    Write-Verbose "ComponentsGeneratedFolder: $folder" -Verbose:$Verbose
    $folder
}

function Get-SubscriptionsJson {
    $file = [System.IO.Path]::Combine((Get-ComponentsFolder), "subscriptions.json")
    Write-Verbose "SubscriptionsJson: $file" -Verbose:$Verbose
    $file
}

function Get-GeneratorFolder {
    $folder = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\Generator")
    Write-Verbose "GeneratorFolder: $folder" -Verbose:$Verbose
    $folder
}

function Get-AppSettingsFolder {
    $folder = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\src\Settings")
    Write-Verbose "AppSettingsFolder: $folder" -Verbose:$Verbose
    $folder
}

function Update-Subscriptions() {
    if (!$ExcelFile) {
        throw "-ExcelFile is required. It must point to a downloaded copy of 'Codespaces Subscription Tracker.xslx'. See https://microsoft.sharepoint.com/teams/VSSaaS/Shared%20Documents/Cloud%20Workspaces/Codespaces%20Subscription%20Tracker.xlsx."
    }

    $ExcelFile = [System.IO.Path]::GetFullPath($ExcelFile)
    Write-Verbose "ExcelFile: $ExcelFile" -Verbose:$Verbose

    Format-ExcelToJson -ExcelFile $ExcelFile -JsonFile (Get-SubscriptionsJson)
}

function Update-Components() {
    $GeneratorFolder = Get-GeneratorFolder
    $ComponentsGeneratedFolder = Get-ComponentsGeneratedFolder
    $ComponentsFolder = Get-ComponentsFolder

    try {
        Push-Location -Path $GeneratorFolder | Out-Null

        Write-Verbose "Invoking npm install for the generator" -Verbose:$Verbose
        & npm install
        if ($LASTEXITCODE -ne 0) {
            throw "npm install failed"
        }

        if (Test-Path -Path $ComponentsGeneratedFolder -PathType Container) {
            Write-Verbose "Deleting components generated folder: $ComponentsGeneratedFolder" -Verbose:$Verbose
            Remove-Item -Path $ComponentsGeneratedFolder -Recurse -Force | Out-Null
        }

        Write-Verbose "Invoking the generator with -i $ComponentsFolder -o $ComponentsGeneratedFolder" -Verbose:$Verbose
        & ts-node-script index.ts -i $ComponentsFolder -o $ComponentsGeneratedFolder
    }
    finally {
        Pop-Location | Out-Null
    }
}

function Update-AppSettings {
    $AppSettingsFolder = Get-AppSettingsFolder
    $SubscriptionsJson = Get-SubscriptionsJson

    Write-Verbose "Updating appsettings.subscriptions.dev.jsonc" -Verbose:$Verbose
    Build-AppSettings -Environment 'dev' -InputJsonFile $SubscriptionsJson -OutputPath $AppSettingsFolder

    Write-Verbose "Updating appsettings.subscriptions.ppe-rel.jsonc" -Verbose:$Verbose
    Build-AppSettings -Environment 'ppe' -InputJsonFile $SubscriptionsJson -OutputPath $AppSettingsFolder

    Write-Verbose "Updating appsettings.subscriptions.prod-rel.jsonc" -Verbose:$Verbose
    Build-AppSettings -Environment 'prod' -InputJsonFile $SubscriptionsJson -OutputPath $AppSettingsFolder

    Write-Verbose "Updating appsettings.subscriptions.prod-can.jsonc" -Verbose:$Verbose
    Build-AppSettings -Environment 'prod' -InputJsonFile $SubscriptionsJson -OutputPath $AppSettingsFolder -Canary
}

if (!$UpdateAll -and !$UpdateSubscriptions -and !$UpdateAppSettings -and !$UpdateComponents) {
    Write-Warning "No update switch is specified."
}

if ($UpdateSubscriptions -or $UpdateAll) {
    "Updating subscriptions.json" | Write-Host -ForegroundColor Green
    Update-Subscriptions
    Write-Host
}

if ($UpdateAppSettings -or $UpdateAll) {
    "Updating appsettings.subscriptions.*.jsonc files" | Write-Host -ForegroundColor Green
    Update-AppSettings
    Write-Host
}

if ($UpdateComponents -or $UpdateAll) {
    "Updating the Components.generated folder" | Write-Host -ForegroundColor Green
    Update-Components
    Write-Host
}
