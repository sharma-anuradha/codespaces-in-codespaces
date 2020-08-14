# Onboard-LegacySubscription.ps1

#requires -version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$SubscriptionNames,
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

function Get-Environment([string]$SubscriptionName) {
    foreach ($e in @("dev", "ppe", "prod")) {
        if ($SubscriptionName.Contains($e) -or $SubscriptionName.EndsWith($e)) {
            return $e
        }
    }
    throw "Subscription name doesn't specify env: $SubscriptionName"
}

function Test-IsDataSubscription([string]$SubscriptionName) {
    return $SubscriptionName.Contains("-data-")
}

function Test-IsLegacySubscription([string]$SubscriptionName) {
    return $SubscriptionName.StartsWith("vsclk-")
}

function Invoke-OnboardLegacySubscription([string]$SubscriptionName) {

    # Validate that the subscription exists
    $sub = Select-AzSubscription -Subscription $SubscriptionName
    $env = Get-Environment -SubscriptionName $SubscriptionName
    Write-Host "Onboarding legacy subscription '$($sub.Subscription.Name)' for environment '$env'" -ForegroundColor Green

    # Get the service principals
    Write-Host "Getting service principals for $env..." -ForegroundColor Green
    $opsSp = Get-AdSp -DisplayName "vsclk-online-$env-devops-sp"
    $appSp = Get-AdSp -DisplayName "vsclk-online-$env-app-sp"

    if (!$SkipRegistrations) {
        $PartitionedData = !(Test-IsLegacySubscription -SubscriptionName $SubscriptionName)
        Register-DefaultProvidersAndFeatures -PartitionedData $PartitionedData
    }

    # Assign RBAC
    Write-Host "Assigning break-glass access control" -ForegroundColor Green
    New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $breakGlassGroup | Out-Null

    Write-Host "Assigning service principal access control" -ForegroundColor Green
    New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $appSp | Out-Null

    if (!(Test-IsDataSubscription -SubscriptionName $SubscriptionName)) {
        New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $opsSp | Out-Null
    }

    if ($env -eq "dev") {
        Write-Host "Assigning team access control for dev" -ForegroundColor Green
        New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $adminsGroup | Out-Null
        New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $contributorsGroup | Out-Null
        New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $readersGroup | Out-Null
    }
    elseif ($env -eq "ppe") {
        Write-Host "Assigning team access control for ppe" -ForegroundColor Green
        New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $adminsGroup | Out-Null
        New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $contributorsGroup | Out-Null
        New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $readersGroup | Out-Null
    }
    elseif ($env -eq "prod") {
    }
}

# User must be Owner to run this script on the subscription.
# Assert-SignedInUserIsOwner | Out-Null

# Get the group accounts for subscription RBAC.
Write-Host "Getting group security accounts..." -ForegroundColor Green
$script:adminsGroup = Get-AdGroup -DisplayName "vsclk-core-admin-a98a"
$script:breakGlassGroup = Get-AdGroup -DisplayName "vsclk-core-breakglass-823b"
$script:contributorsGroup = Get-AdGroup -DisplayName "vsclk-core-contributors-3a5d"
$script:readersGroup = Get-AdGroup -DisplayName "vsclk-core-readers-fd84"

$SubscriptionNames | % {
    Invoke-OnboardLegacySubscription -SubscriptionName $_
}
