# Invoke-GeneratedValidators.ps1

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

function Get-Validators() {
    $root = Get-ComponentsGeneratedFolder
    if (!$All) {
        $root = Join-Path $root $Component
    }
    if (!(Test-Path $root -PathType Container)) {
        throw "Root path does not exist: $root"
    }

    $validationDirectories = Get-ChildItem -Path $root -Recurse -Directory -Filter Validation |
    ForEach-Object { [System.IO.Path]::GetFullPath($_) }

    Write-VerboseInfo "Validation Directories"
    $validationDirectories | Out-String | Write-VerboseInfo

    $validators = @()
    foreach ($dir in $validationDirectories) {
        $scripts = Get-ChildItem -Path $dir -Recurse -File -Filter '*.ps1' |
        ForEach-Object { [System.IO.Path]::GetFullPath($_) }

        foreach ($script in $scripts) {
            $validators += $script
        }
    }

    $validators = $validators | Sort-Object

    Write-VerboseInfo "Validators"
    $validators | Out-String | Write-VerboseInfo
    $validators
}

function Main() {
    $errors = $false
    $validators = Get-Validators
    foreach ($validator in $validators) {
        Write-Host "Invoking validator '$validator'" -ForegroundColor Green

        try {
            . "$validator" -Verbose:$script:Verbose
        }
        catch {
            Write-Host "Validation failed for '$validator': $_.Exception.Message" -ForegroundColor Red
            $errors = $true
            if ($FailFast) {
                throw
            }
        }
    }

    if ($errors) {
        throw "One or more validators failed"
    }
}

Main
