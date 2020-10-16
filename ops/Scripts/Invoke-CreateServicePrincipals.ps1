# Invoke-CreateServicePrincipals.ps1

#requires -version 7.0

[CmdletBinding(DefaultParameterSetName = 'default')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'default')]
    [string]$Component,
    [Parameter(Mandatory = $true, ParameterSetName = 'all')]
    [switch]$All,
    [switch]$FailFast
)

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:Verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

function Write-VerboseInfo {
    [CmdletBinding(DefaultParameterSetName = 'default')]
    param (
        [Parameter(Mandatory = $false, ValueFromPipeline = $true, ParameterSetName = 'pipeline')] $InputObject,
        [Parameter(Position = 0, Mandatory = $false, ParameterSetName = 'default')] $Message
    )

    if (!$Message -and $null -ne $InputObject) {
        $message = $InputObject | Out-String
    }

    $message | Out-String | Write-Verbose -Verbose:$script:Verbose
}

function Get-ComponentsFolder {
    $folder = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\Components")
    Write-VerboseInfo "ComponentsFolder: $folder" -Verbose:$Verbose
    $folder
}

function Get-ComponentsGeneratedFolder {
    $folder = "$(Get-ComponentsFolder).generated"
    Write-VerboseInfo "ComponentsGeneratedFolder: $folder" -Verbose:$Verbose
    $folder
}

function Get-Scripts() {
    $root = Get-ComponentsGeneratedFolder
    if (!$All) {
        $root = Join-Path $root $Component
    }

    Write-VerboseInfo "Script root: $root"
    if (!(Test-Path $root -PathType Container)) {
        throw "Root path does not exist: $root"
    }

    $directories = Get-ChildItem -Path $root -Recurse -Directory | ForEach-Object { [System.IO.Path]::GetFullPath($_) }
    Write-VerboseInfo "Directories"
    $directories | Out-String | Write-VerboseInfo

    $allScripts = @()
    foreach ($dir in $directories) {
        $scripts = Get-ChildItem -Path $dir -Recurse -File -Filter '*.createserviceprincipals.ps1' |
            ForEach-Object { [System.IO.Path]::GetFullPath($_) }

        foreach ($script in $scripts) {
            Write-VerboseInfo "Adding script: $script"
            $allScripts += $script
        }
    }

    $allScripts = $allScripts | Sort-Object
    $allScripts
}

function Main() {
    $errors = $false
    $scripts = Get-Scripts
    foreach ($script in $scripts) {
        Write-Host "Invoking script '$script'" -ForegroundColor Green

        try {
            . "$script" -Verbose:$script:Verbose
        }
        catch {
            Write-Host "Script failed '$script': $_.Exception.Message" -ForegroundColor Red
            $errors = $true
            if ($FailFast) {
                throw
            }
        }
    }

    if ($errors) {
        throw "One or more scripts failed"
    }
}

Main
