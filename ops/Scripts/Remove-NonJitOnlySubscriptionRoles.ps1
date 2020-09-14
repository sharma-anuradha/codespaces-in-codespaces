# Onboard-LegacySubscription.ps1

#requires -version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, ParameterSetName="Names")]
    [string[]]$SubscriptionNames,
    [Parameter(Mandatory = $true, ParameterSetName="Query")]
    [switch]$All,
    [switch]$AllowNonProd,
    [switch]$AllowNonVscs,
    [switch]$WhatIf,
    [switch]$Confirm
)

# Global error handling
trap {
    Write-Error $_
    exit 1
}

# Utilities
# . "$PSScriptRoot\OpsUtilities.ps1"

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
$script:whatif = $false
$script:confirm = $confirm
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}
if ($PSBoundParameters.ContainsKey('WhatIf')) {
    $script:whatif = $PsBoundParameters.Get_Item('WhatIf')
    $WhatIfPreference = $script:whatif
}
if ($PSBoundParameters.ContainsKey('Confirm')) {
    $script:confirm = $PsBoundParameters.Get_Item('Confirm')
}

function Test-ShouldRemoveNonJitRole() {
    [OutputType([bool])]
    param (
        [Microsoft.Azure.Commands.Resources.Models.Authorization.PSRoleAssignment]$Role,
        [string]$Scope
    )

    if ($Role.Scope -ne $Scope) {
        return $false
    }

    if ($Role.ObjectType -eq "Group") {
        if ($Role.DisplayName.StartsWith('ESJIT-')) {
            return $false
        }
        if ($Role.DisplayName.StartsWith('BG-')) {
            return $false
        }
        if ($Role.DisplayName.Contains('breakglass')) {
            return $false
        }
    }

    if ($Role.ObjectType -eq "ServicePrincipal") {
        return $false
    }

    return $true
}

function Remove-NonJitOnlySubscriptionRolesInternal([string]$SubscriptionName) {

    # Validate that the subscription exists and get the id
    try {
        $sub = Get-AzSubscription -SubscriptionName $SubscriptionName
        $subscriptionId = $sub.Id
    }
    catch {
        throw "Subscription not found: ${SubscriptionName}: $_"
    }

    # Get all roles
    $scope = "/subscriptions/$subscriptionId"
    $roles = Get-AzRoleAssignment -Scope $scope

    # Filter to non-JIT roles
    $rolesToRemove = $roles | Where-Object { Test-ShouldRemoveNonJitRole -Role $_ -Scope $scope }

    # Remove roles
    if ($rolesToRemove) {
        "Removing non-JIT roles from ${SubscriptionName}" | Write-Host -ForegroundColor Green
        foreach ($role in $rolesToRemove) {
            "Removing role '$($role.RoleDefinitionName)' from '$($role.DisplayName)' ($($role.ObjectId))" | Write-Verbose -Verbose:$verbose
            $role | Remove-AzRoleAssignment -WhatIf:$whatif -Confirm:$confirm -Verbose:$verbose
        }
    }
}


if ($All) {
    $SubscriptionNames = Get-AzSubscription | Select-Object -ExpandProperty Name
}

$SubscriptionNames = $SubscriptionNames | Sort-Object

$VscsOnly = !$AllowNonVscs
if ($VscsOnly) {
    $skipped = $SubscriptionNames | Where-Object { !$_.StartsWith('vscs-') }
    $SubscriptionNames = $SubscriptionNames | Where-Object { $_.StartsWith('vscs-') }
    if ($skipped) {
        "Skipping non-vscs subscriptions:" | Write-Verbose -Verbose:$verbose
        $skipped | Write-Verbose -Verbose:$verbose
        Write-Host
    }
}

$ProdOnly = !$AllowNonProd
if ($ProdOnly) {
    $skipped = $SubscriptionNames | Where-Object { !$_.Contains('-prod-') }
    $SubscriptionNames = $SubscriptionNames | Where-Object { $_.Contains('-prod-')}
    if ($skipped) {
        "Skipping non-production subscriptions:" | Write-Verbose -Verbose:$verbose
        $skipped | Write-Verbose -Verbose:$verbose
        Write-Host
    }
}

"Removing non-JIT roles from subscriptions:" | Write-Verbose -Verbose:$verbose
$SubscriptionNames | Write-Verbose -Verbose:$verbose
if ($verbose) {
    Write-Host
}

foreach ($SubscripionName in $SubscriptionNames) {
    Remove-NonJitOnlySubscriptionRolesInternal -SubscriptionName $SubscripionName
}
