#requires -version 7.0

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

. ".\Subscription-Tracker.ps1"

$devSubscriptions = Get-Subscriptions -Environment:"dev" -Plane:"data" -UseAppSettingsFilters:$true

Write-Host
Write-Host "Full list of subscriptions to validate:"
$devSubscriptions | Select-Object subscriptionId | Write-Host
Write-Host

$validDevSubscriptions = $devSubscriptions | Test-All
$invalidDevSubscriptions = $devSubscriptions | Where-Object { $validDevSubscriptions -notcontains $_ }

Write-Host
Write-Host "Valid Subscriptions:"
$validDevSubscriptions | Select-Object subscriptionId | Write-Host
Write-Host

Write-Host
Write-Host "Invalid Subscriptions:"
$invalidDevSubscriptions | Select-Object subscriptionId | Write-Host
Write-Host