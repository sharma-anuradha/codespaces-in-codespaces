# OpsUtilities.ps1

#requires -version 7.0
#requires -modules @{ ModuleName = "Az.Accounts"; ModuleVersion="1.7.4" }
#requires -modules @{ ModuleName= "Az.KeyVault"; ModuleVersion="1.5.2"}
#requires -modules @{ ModuleName = "Az.Resources"; ModuleVersion="1.13.0" }

# Module dependencies
Import-Module "Az.Accounts" -Verbose:$false
Import-Module "Az.Keyvault" -Verbose:$false
Import-Module "Az.Resources" -Verbose:$false

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
  $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

# Global error handling
trap {
  Write-Error $_
  exit 1
}

# Constants
$script:DefaultPrefix = "vscs"

# Geographies
$script:AzureGeographies = @(
  "ae"
  "ap"
  "au"
  "br"
  "ca"
  "ch"
  "cn"
  "de"
  "eu"
  "fr"
  "in"
  "jp"
  "kr"
  "no"
  "sa"
  "uk"
  "us"
)

function Test-AzureGeography([string]$Geo, [switch]$Throw) {
  $valid = $Geo -in $script:AzureGeographies

  if (!$valid) {
    if ($Throw) {
      throw "Invalid azure geography: $Geo"
    }
  }

  $valid
}

# Get the azure subscription name for the various dimensions
function Get-AzureSubscriptionName(
  [string]$Prefix,
  [Parameter(Mandatory = $true)]
  [string]$Component,
  [Parameter(Mandatory = $true)]
  [string]$Env,
  [Parameter(Mandatory = $true)]
  [string]$Plane,
  [string]$DataType,
  [string]$Geo,
  [string]$RegionCode,
  [int]$Count) {

  $subscriptionName = Get-AzureResourceName -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane

  # Hack for vscs-core-test, which doesn't have associated "plane" subscriptions
  if ($Env -eq "test") {
    $subscriptionName = $subscriptionName.Replace("-$Plane", "", 5)
    return $subscriptionName
  }

  if ($Plane -eq "data") {

    if ($DataType) {
      $subscriptionName += "-$DataType"
    }
  
    if ($Geo -and $RegionCode) {
      Test-AzureGeography -Geo $Geo -Throw
      $subscriptionName += "-$Geo-$RegionCode"
    }
    else {
      throw "Both geo and regioncode are requied for data subscriptions."
    }
   
    $countSuffix = "{0:d3}" -f $count
    $subscriptionName += "-$countSuffix"
  }


  $subscriptionName = $subscriptionName.ToLowerInvariant()

  $subscriptionName
}

function Get-AzureResourceName(
  [string]$Prefix,
  [Parameter(Mandatory = $true)]
  [string]$Component,
  [Parameter(Mandatory = $true)]
  [string]$Env,
  [Parameter(Mandatory = $true)]
  [string]$Plane,
  [string]$Instance,
  [string]$Stamp,
  [string]$TypeSuffix,
  [bool]$NoHyphens) {

  if (!$Prefix) {
    $Prefix = $script:DefaultPrefix
  }

  $resourceName = "$Prefix-$Component-$Env-$Plane"

  if ($Instance) {
    $resourceName += "-$Instance"
  }

  if ($Stamp) {
    $resourceName += "-$Stamp"
  }

  if ($TypeSuffix) {
    $resourceName += "-$TypeSuffix"
  }

  if ($NoHyphens) {
    $resourceName = $resourceName.Replace("-", "")
  }

  $resourceName = $resourceName.ToLowerInvariant()

  $resourceName
}

function Get-AzureResourceStampName(
  [Parameter(Mandatory = $true)]
  [string]$Geo,
  [Parameter(Mandatory = $true)]
  [string]$RegionSuffix) {

  Test-AzureGeography -Geo $Geo -Throw

  if ($RegionSuffix.Length -gt 3) {
    throw "Region suffix maximum length is 3"
  }

  "$Geo-$RegionSuffix"
}


# Key Vault: Set OneCert Issuer
function Set-AzKeyVaultCertificateIssuerOneCert(
  [string]$VaultName) {
  Set-AzKeyVaultCertificateIssuer -VaultName $VaultName -Name "OneCert" -IssuerProvider "OneCert"
  Get-AzKeyVaultCertificateIssuer -VaultName $VaultName -Name "OneCert"
}

# Certificate issuer policy using OneCert
function New-AzKeyVaultOneCertPolicy([string]$SubjectCName) {

  $registeredDomain = "online.core.vsengsaas.visualstudio.com"

  $policyParams = @{
    Ekus                            = @( "1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.2" )
    IssuerName                      = "OneCert"
    KeySize                         = 2048
    KeyType                         = "RSA"
    KeyUsage                        = "DigitalSignature"
    KeyNotExportable                = $false
    RenewAtNumberOfDaysBeforeExpiry = 180
    ReuseKeyOnRenewal               = $false
    SecretContentType               = "application/x-pem-file"
    SubjectName                     = "CN=${SubjectCName}.${registeredDomain}"
    ValidityInMonths                = 24
  }

  New-AzKeyVaultCertificatePolicy @policyParams
}

function Add-AzKeyVaultOneCertCertificate(
  [string]$VaultName,
  [string]$Name) {

  Set-AzKeyVaultCertificateIssuerOneCert -VaultName $VaultName | Out-Null

  $cert = Get-AzKeyVaultCertificate -VaultName $VaultName -Name $Name

  if (!$cert) {
    $policy = New-AzKeyVaultOneCertPolicy -SubjectCName $Name
    $cert = Add-AzKeyVaultCertificate -VaultName $VaultName -Name $Name -CertificatePolicy $policy
  }

  $cert
}

function Get-ServicePrincipal(
  [string]$Prefix,
  [Parameter(Mandatory = $true)]
  [string]$Component,
  [Parameter(Mandatory = $true)]
  [string]$Env,
  [Parameter(Mandatory = $true)]
  [string]$Plane
) {
  $servicePrincipalName = Get-AzureResourceName -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane -TypeSuffix "sp"
  Get-AzADServicePrincipal -DisplayName $servicePrincipalName
}

function Reset-AppCertificateCredential(
  [string]$appId,
  [string]$VaultName,
  [string]$CertName) {

  $cert = Get-AzKeyVaultCertificate -VaultName $vaultName -Name $certName
  $expires = $cert.Expires
  $expiresUtc = $expires.ToString("u")

  # PowerShell doesn't have a commandlet for doing this :(
  $subscriptionName = (Get-AzContext).Subscription.Name
  & az account set --subscription $subscriptionName
  if ($LASTEXITCODE -ne 0) {
    throw "az account set failed"
  }
  
  & az ad app credential reset --id $appId --append --keyvault $vaultName --cert $certName --credential-description "${vaultName}:${certName}" --end-date $expiresUtc
  if ($LASTEXITCODE -ne 0) {
    throw "az ad app credential reset failed"
  }
}