# OnboarDataSub

param (
    [Parameter(Mandatory = $True)]
    [string] $subscriptionName,
    [Parameter(Mandatory = $True)]
    [ValidateSet("dev", "ppe", "prod")]
    [string] $env
)

#requires -version 5.1
#requires -modules @{ ModuleName = "Az.Resources"; ModuleVersion="2.0.1" }

# This trap will handle all errors.
# There should be no need to use a catch below in this script, unless you want to ignore a specific error.
trap {
    Write-Error "$_"
    exit -1
}

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    # Command line specifies -Verbose[:$false]
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

# Security Group Names
$script:adminGroupName = "vsclk-core-admin-a98a"
$script:breakglassGroupName = "vsclk-core-breakglass-823b"
$script:contributorsGroupName = "vsclk-core-contributors-3a5d"
$script:readersGroupName = "vsclk-core-readers-fd84"

# App Service Principal Name
$script:appSpName = "vsclk-online-$env-app-sp"

function Set-AzRoleAssignmentGroup([string]$DisplayName, [string]$RoleDefinitionName) {
    Write-Host "Setting group $DisplayName as $RoleDefinitionName" -ForegroundColor Green
    $group = Get-AzADGroup -DisplayName $DisplayName
    Set-AzRoleAssignment -ObjectId $group.Id -RoleDefinitionName $RoleDefinitionName
}

function Set-AzRoleAssignmentServicePrincipal([string]$DisplayName, [string]$RoleDefinitionName) {
    Write-Host "Setting service principal $DisplayName as $RoleDefinitionName" -ForegroundColor Green
    $sp = Get-AzADServicePrincipal -DisplayName $DisplayName
    Set-AzRoleAssignment -ObjectId $sp.Id -RoleDefinitionName $RoleDefinitionName
}

function Set-AzRoleAssignment([string]$ObjectId, [string]$RoleDefinitionName) {
    $role = Get-AzRoleAssignment -ObjectId $ObjectId -RoleDefinitionName $RoleDefinitionName -scope $script:subscriptionScope
    if (!$role) {
        $role = New-AzRoleAssignment -ObjectId $ObjectId -RoleDefinitionName $RRoleDefinitionNameole -scope $script:subscriptionScope
    }

    Write-Verbose "$($role.DisplayName) : $($role.RoleDefinitionName)" -Verbose:$verbose
    $role
}

# Validate Subscription
$sub = Get-AzSubscription -SubscriptionName $subscriptionName
$script:subscriptionId = $sub.Id
Write-Host "Onboarding subscription '$($sub.Name)' (${script:subscriptionId})" -ForegroundColor Green

# Set Subscription Scope
$script:subscriptionScope = "/subscriptions/$subscriptionId"

Set-AzRoleAssignmentGroup -DisplayName $script:breakglassGroupName -RoleDefinitionName "Owner" | Out-Null
Set-AzRoleAssignmentGroup -DisplayName $script:readersGroupName -RoleDefinitionName "Reader" | Out-Null
Set-AzRoleAssignmentServicePrincipal -DisplayName $script:appSpName -RoleDefinitionName "Contributor" | Out-Null

if ($env -eq "dev") {
    Set-AzRoleAssignmentGroup -DisplayName $script:adminGroupName -RoleDefinitionName "Owner" | Out-Null
    Set-AzRoleAssignmentGroup -DisplayName $script:contributorsGroupName -RoleDefinitionName "Owner" | Out-Null
    Set-AzRoleAssignmentGroup -DisplayName $script:contributorsGroupName -RoleDefinitionName "Contributor" | Out-Null
}
