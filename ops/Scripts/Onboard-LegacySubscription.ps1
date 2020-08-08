# Onboard-LegacySubscription.ps1

#requires -version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SubscriptionName,
    [switch]$SkipRegistrations
)

# Global error handling
trap {
    Write-Error $_
    exit 1
}

# Utilities
. ".\OpsUtilities.ps1"

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

function Get-AdSp([string]$DisplayName) {
    $sp = Get-AzADServicePrincipal -DisplayName $DisplayName
    if (!$sp) {
        throw "Service principal does not exist: $DisplayName"
    }
    "$($sp.DisplayName) $($sp.Id)" | Out-String | Write-Host -ForegroundColor DarkGray
    $sp
}

function Get-AdGroup([string]$DisplayName) {
    $group = Get-AzADGroup -DisplayName $DisplayName
    if (!$group) {
        throw "Group does not exist: $DisplayName"
    }
    "$($group.DisplayName) $($group.Id)" | Out-String | Write-Host -ForegroundColor DarkGray
    $group
}

function Get-Environment() {
    foreach ($e in @("dev", "ppe", "prod")) {
        if ($SubscriptionName.Contains($e) -or $SubscriptionName.EndsWith($e)) {
            return $e
        }
    }
    throw "Subscription name doesn't specify env: $$SubscriptionName"
}

function Test-Environment([string]$e) {
    return ${Env} -eq $e
}

function Test-IsDataSubscription() {
    return $SubscriptionName.Contains("-data-")
}

function Test-IsLegacySubscription() {
    return $SubscriptionName.StartsWith("vsclk-")
}


# Validate that the subscription exists
$sub = Select-AzSubscription -Subscription $SubscriptionName
$Env = Get-Environment
Write-Host "Onboarding legacy subscription '$($sub.Subscription.Name)' for environment '$Env'" -ForegroundColor Green

# User must be Owner to run this script on the subscription.
# Assert-SignedInUserIsOwner | Out-Null

if (!$SkipRegistrations) {
    $PartitionedData = !(Test-IsLegacySubscription)
    Register-DefaultProvidersAndFeatures -PartitionedData $PartitionedData
}

# Get the service principals
Write-Host "Getting service principals..." -ForegroundColor Green
$opsSp = Get-AdSp -DisplayName "vsclk-online-$env-devops-sp"
$appSp = Get-AdSp -DisplayName "vsclk-online-$env-app-sp"

# Get the group accounts for subscription RBAC.
Write-Host "Getting group security accounts..." -ForegroundColor Green
$adminsGroup = Get-AdGroup -DisplayName "vsclk-core-admin-a98a"
$breakGlassGroup = Get-AdGroup -DisplayName "vsclk-core-breakglass-823b"
$contributorsGroup = Get-AdGroup -DisplayName "vsclk-core-contributors-3a5d"
$readersGroup = Get-AdGroup -DisplayName "vsclk-core-readers-fd84"

# Assign RBAC
Write-Host "Assigning break-glass access control" -ForegroundColor Green
New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $breakGlassGroup | Out-Null

Write-Host "Assigning service principal access control" -ForegroundColor Green
New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $appSp | Out-Null

if (!(Test-IsDataSubscription("data"))) {
    New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $opsSp | Out-Null
}

if (Test-Environment("dev")) {
    Write-Host "Assigning team access control for dev" -ForegroundColor Green
    New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $adminsGroup | Out-Null
    New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $contributorsGroup | Out-Null
    New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $readersGroup | Out-Null
}
elseif (Test-Environment("ppe")) {
    Write-Host "Assigning team access control for ppe" -ForegroundColor Green
    New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $adminsGroup | Out-Null
    New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $contributorsGroup | Out-Null
    New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $readersGroup | Out-Null
}
elseif (Test-Environment("prod")) {
}
