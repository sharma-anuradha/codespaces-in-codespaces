# Auto-Generated From Template
# "codesp.createserviceprincipals.ps1"
# with names file "codesp.ppe-data.names.json"
# from template file "CreateServicePrincipals.ps1"
# Do not edit this generated file. Edit the source file and rerun the generator instead.

# CreateComponentServicePrincipals.ps1

#requires -version 7.0
#requires -modules @{ ModuleName = "Az.Accounts"; ModuleVersion="1.7.4" }

[CmdletBinding()]
param(
    [string]$Prefix = "vscs",
    [string]$Component = "codesp",
    [string]$Env = "ppe",
    [string]$TenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d",
    [string]$Plane = "data"
)

# Module dependencies
Import-Module "Az.Accounts" -Verbose:$false

# Preamble
Set-StrictMode -Version Latest

$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

function Get-ServicePrincipal(
    [string]$servicePrincipalName
) {
    $sp = Get-AzADServicePrincipal -DisplayName $servicePrincipalName
    if (!$sp) {
        $sp = New-AzADServicePrincipal -DisplayName $servicePrincipalName -SkipAssignment
        if (!$sp) {
            throw "failed to create service principal $servicePrincipalName"
        }
    }

    $sp
}

function Get-Environment() {
    $currentTenant = (Get-AzContext).Tenant.id
    if ($currentTenant -eq $TenantId) {
        Write-Host "Processing environment '$env' in tenant '$TenantId'"
        return $env
    }
    else {
        Write-Warning "Current tenant '$currentTenant' does not match configured tenant '$TenantId'."
        Write-Warning "Skipping environment '$env'."
        return $null
    }
}

if ($Plane -ne 'data') {
    $env = Get-Environment
    if ($env) {
        Write-Host
        $servicePrincipalName = "$Prefix-$Component-$Env-$Plane-sp"
        Write-Host "Ensuring service principal '$servicePrincipalName' exists..."
        $sp = Get-ServicePrincipal -servicePrincipalName $servicePrincipalName
        $sp | Out-String | Write-Host -ForegroundColor DarkGray
    }
}
else {
    Write-Warning "Skipping service principal for plane '$Plane"
}
