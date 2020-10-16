# New-AzureSubscription.ps1
# https://microsoft.sharepoint.com/teams/azureinternal/Pages/Creating%20internal%20subscriptions%20using%20API.aspx
# https://github.com/Azure/azure-rest-api-specs/blob/master/specification/subscription/resource-manager/Microsoft.Subscription/preview/2018-03-01-preview/subscriptions.json

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SubscriptionName,
    [string]$ServiceTreeMetadata
)

#requires -version 7.0
#requires -modules @{ ModuleName = "Az.Accounts"; ModuleVersion="1.9.2" }
#requires -modules @{ ModuleName = "Az.Resources"; ModuleVersion="2.4.0" }

# Module dependencies
Import-Module "Az.Accounts" -Verbose:$false
Import-Module "Az.Resources" -Verbose:$false

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
  $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

function Test-ArmClientResult($result) {
    if ($LASTEXITCODE -ne 0) {
        $message = $result | Out-String
        throw $message
    }
}

$ameTenant = "33e01921-4d64-4f8c-a055-5bdaffd5e33d"
if ($ameTenant -ne (Get-AzContext).Tenant.Id) {
    throw "You must be signed in using your @ame.gbl user account. Use Login-AzAccount."
}

$sub = Get-AzSubscription -SubscriptionName $SubscriptionName -TenantId $ameTenant
if ($sub) {
    throw "Subscription already exists in the AME tenant: $SubscriptionName ($($sub.Id))"
}

if (!$ServiceTreeMetadata) {
    $subscriptionRegisterUrl = 'https://servicetree.msftcloudes.com/?pathSet=true#/subscription-register/'
    Start-Process $subscriptionRegisterUrl
    throw "-ServiceTreeMetadata is required. Copy from $subscriptionRegisterUrl -- sign in with your @ame.gbl user account. Paste using single-quotes '{...}'."
}
try {
    ConvertFrom-Json $ServiceTreeMetadata | Out-Null
}
catch {
    throw "Invalid service tree metadata: $_"
}

# ArmClient.exe
$armClient = Get-Command "ARMClient.exe"
if (!$armClient) {
    throw "armclient.exe is not installed. Use 'choco install armclient'."
}
$armClientExe = $armClient.Source
$armClientTenant = (& $armClientExe token | Where-Object { !$_.StartsWith('Token') } | ConvertFrom-Json).tid
Test-ArmClientResult $armClientTenant
if ($armClientTenant -ne $ameTenant) {
    throw "armclient.exe is not authenticated into the AME tenant. Use 'armclient login' with your @ame.glb user account."
}

# Check enrollment accounts
$msAZR15P = "MS-AZR-0015P"
$enrollmentAccounts = & $armClientExe GET providers/Microsoft.Billing/enrollmentAccounts?api-version=2018-03-01-preview
Test-ArmClientResult $enrollmentAccounts
$enrollmentAccounts = $enrollmentAccounts | ConvertFrom-Json
$accountName = $enrollmentAccounts.value.name
$offerTypes = $enrollmentAccounts.value.properties.offerTypes

$enrollmentAccount = & $armClientExe GET providers/Microsoft.Billing/enrollmentAccounts/${accountName}?api-version=2018-03-01-preview
Test-ArmClientResult $enrollmentAccount
$enrollmentAccount | Out-String | Write-Host -ForegroundColor DarkGray

if (!($msAZR15P -in $offerTypes)) {
    throw "Offer type $msAZR15P is not supported"
}

$Production = $SubscriptionName.Contains("-prod-")
if ($Production) {
    $PCCode = "P74841"
}
else {
    $PCCode = "P10168064"
}

$adminObjectId = (Get-AzADGroup -DisplayName 'AD-Codespaces').Id
$breakGlassObjectId = (Get-AzADGroup -DisplayName 'BG-Codespaces').Id

$payload = @{
    offerType = $msAZR15P
    displayName = $SubscriptionName
    owners = @( @{ objectId = $adminObjectId }, @{ objectId = $breakGlassObjectId} )
    additionalParameters = @{
        PCCode = $PCCode
        CostCategory = ""
        ServiceTreeMetadata = $ServiceTreeMetadata
        SubscriptionTenantId = $ameTenant
    }
}
$payloadJson = $payload | ConvertTo-Json -Depth 10
$payloadJson | Write-Host -ForegroundColor DarkGray
$jsonFile = (New-TemporaryFile).FullName
Set-Content -Path $jsonFile -Value $payloadJson -Encoding utf8
try {
    $result = & $armClientExe POST providers/Microsoft.Billing/enrollmentAccounts/$accountName/providers/Microsoft.Subscription/createSubscription?api-version=2019-10-01-preview @$jsonFile -verbose
    Test-ArmClientResult $result
    $result | Out-String | Write-Host -ForegroundColor DarkGray
    $locationLine = $result | Where-Object { $_.StartsWith('Location: https://management.azure.com/') } | Select-Object -First 1
    $location = $locationLine.Replace('Location: https://management.azure.com/','')
    "armclient GET $location" | Out-String | Write-Host -ForegroundColor Yellow
    & $armClientExe GET $location
}
finally {
    Remove-Item -Path $jsonFile
}
