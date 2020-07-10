# Assign-ServicePrincipalCert.ps1
# Assign certificate credentials to a service principal.

#requires -version 7.0

[CmdletBinding()]
param(
    [string]$Prefix,
    [Parameter(Mandatory = $true)]
    [string]$Component,
    [Parameter(Mandatory = $true)]
    [string]$Env,
    [Parameter(Mandatory = $true)]
    [string]$Plane
)

#Global error handling
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

# Names
$subscriptionName = Get-AzureSubscriptionName -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane
$vaultName = Get-AzureResourceName -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane -TypeSuffix "kv"
# Temporary hack because the original key vault couldn't be deleted/purged.
if ($vaultName -eq "vscs-core-test-ops-kv") {
    $vaultName += "2"
}

$servicePrincipalName = Get-AzureResourceName -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane -TypeSuffix "sp"
$certName = $servicePrincipalName

# Action
Select-AzSubscription -Subscription $subscriptionName | Out-Null
$sp = Get-ServicePrincipal -Prefix $Prefix -Component $Component -Env $Env -Plane $plane
$appId = $sp.ApplicationId
Reset-AppCertificateCredential -AppId $appid -VaultName $vaultName -CertName $certName
