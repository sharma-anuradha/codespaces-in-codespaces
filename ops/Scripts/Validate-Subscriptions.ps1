#requires -version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Env
)

# Auto-install modules as necessary, so this can be run from AzDO pipelines
foreach ($module in @("Az.Accounts", "Az.Keyvault", "Az.Resources")) {
    if (-not (Get-Module $module)) {
        Install-Module -Name $module -Scope CurrentUser -Force -AllowClobber
    }
}

$reportError = $false

$opsDir = Resolve-Path (Join-Path $PSScriptRoot ..)

. "$opsDir\Scripts\Subscription-Tracker.ps1"

function Select-ValidationResults {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Subscription[]]$Subscriptions
    )

    Process {
        $Subscriptions | Select-Object @{ label="svr"; expression={$_.SelectValidationResults()} } | Select-Object -ExpandProperty svr
    }
}

$subscriptions = Get-Subscriptions -Environment:$Env -Plane:"data" -UseAppSettingsFilters:$true -SubscriptionJsonFile:"$opsDir\Components.generated\subscriptions.json"

Write-Host
Write-Host "Full list of subscriptions to validate:"
$subscriptions | Select-Object subscriptionName, subscriptionId | Format-Table
Write-Host

$inaccessibleSubs = @()
foreach ($sub in $subscriptions) {
  $r = az account show -s $sub.subscriptionId | ConvertFrom-Json
  if ($null -eq $r) {
    $inaccessibleSubs += $sub
  }
}

if ($inaccessibleSubs.Count -gt 0) {
    $reportError = $true
    Write-Host
    Write-Host "Inaccessible subscriptions:"
    $inaccessibleSubs | Select-Object subscriptionName, subscriptionId | Format-Table
    Write-Host
}

$accessibleSubscriptions = $subscriptions | Where-Object { $inaccessibleSubs -notcontains $_ }

$testedSubscriptions = $accessibleSubscriptions | Test-All

# Untested subs indicate an error in the powershell pipeline
$untestedSubscriptions = $accessibleSubscriptions | Where-Object { $testedSubscriptions -notcontains $_ }
if ($untestedSubscriptions) {
    $reportError = $true
    Write-Host "Some accessible subscriptions were not fully tested (indicates an error in this script):"
    $untestedSubscriptions | Select-ValidationResults | Format-List
}

$validSubscriptions = $testedSubscriptions | Where-Object { $_.validation.IsValid() }
$invalidSubscriptions = $testedSubscriptions | Where-Object { -not ($_.validation.IsValid()) }

Write-Host
Write-Host "Valid Subscriptions:"
$validSubscriptions | Select-ValidationResults | Format-List
Write-Host

if ($invalidSubscriptions) {
    $reportError = $true
    Write-Host
    Write-Host "Invalid Subscriptions:"
    $invalidSubscriptions | Select-ValidationResults | Format-List
    Write-Host
}

Write-Host
$FinalReport = [ordered]@{
    "Total Subscription Count"=@($subscriptions).Count
    "Inaccessible Subscription Count"=@($inaccessibleSubs).Count
    "Tested Subscription Count"=@($testedSubscriptions).Count
    "Valid Subscription Count"=@($validSubscriptions).Count
    "Invalid Subscription Count"=@($invalidSubscriptions).Count
}
$FinalReport | Format-Table

Write-Host
Write-Host "See log for details (warnings, etc)"

if ($reportError) {
    Write-Error "Validation failed."
}