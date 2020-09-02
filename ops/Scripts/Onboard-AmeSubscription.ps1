# Onboard-Subscription.ps1

#requires -version 7.0

[CmdletBinding()]
param(
    [string]$Prefix = "vscs",
    [Parameter(Mandatory = $true)]
    [string]$Component,
    [Parameter(Mandatory = $true)]
    [string]$Env,
    [Parameter(Mandatory = $true)]
    [string]$Plane,
    [string]$DataType,
    [string]$Geo,
    [string]$RegionCode,
    [int]$Count,
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

# Validate that the subscription exists
$sub = Select-AzureSubscription -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane -DataType $DataType -Geo $Geo -RegionCode $RegionCode -Count $Count
Write-Host "Onboarding subscription '$($sub.Subscription.Name)'" -ForegroundColor Green

# User must be Owner to run this script on the subscription.
Assert-SignedInUserIsOwner | Out-Null

if (!$SkipRegistrations) {
    Register-DefaultProvidersAndFeatures
}

# Get or create the ops service principal, needed for subscription RBAC.
# Except for data plane -- the ops sp should not have access to customer data.
$opsSpId = ""
if ($Plane -ne "data") {
    Write-Host
    Write-Host "Ensuring core-ops service principal exists..."
    $opsSp = Get-ServicePrincipal -Prefix $Prefix -Component "core" -Env $Env -Plane "ops" -Create
    $opsSp | Out-String | Write-Host -ForegroundColor DarkGray
    $opsSpId = $opsSp.Id
}

# Get or create the app service principal, needed for runtime access.
$appSpId = ''
if ($Plane -ne 'ops') {
    Write-Host
    Write-Host 'Ensuring app service principal exists...'
    $appSp = Get-ServicePrincipal -Prefix $Prefix -Component $Component -Env $Env -Plane 'ctl' -Create
    $appSp | Out-String | Write-Host -ForegroundColor DarkGray
    $appSpId = $opsSp.Id
}

# Get or create the first-party app service principal, needed for runtime access in compute subs.
$firstPartyAppSpId = ''
if ($Plane -eq 'data' -and $DataType -eq 'compute') {
    Write-Host
    Write-Host 'Getting first-party app service principal...'
    if ($Env -eq 'ppe' -or $Env -eq 'prod') {
        $appId = '9bd5ab7f-4031-4045-ace9-6bebbad202f6'
    }
    else {
        $appId = '48ef7923-268f-473d-bcf1-07f0997961f4'
    }
    $firstPartyAppSpId = Get-AzADServicePrincipal -ApplicationId $appId
    $firstPartyAppSpId | Out-String | Write-Host -ForegroundColor DarkGray
    $firstPartyAppSpId = $firstPartyAppSpId.Id
}

# Get the group accounts for subscription RBAC.
Write-Host "Getting group security accounts..."
if ($env -ne "ppe" -and $env -ne "prod") {
    $adminsGroup = Get-AzADGroup -DisplayName "vsclk-core-admin-a98a"
    $adminsGroup | Out-String | Write-Host -ForegroundColor DarkGray
    $adminsGroup = $adminsGroup.Id
    $breakGlassGroup = Get-AzADGroup -DisplayName "vsclk-core-breakglass-823b"
    $breakGlassGroup | Out-String | Write-Host -ForegroundColor DarkGray
    $breakGlassGroup = $breakGlassGroup.Id
    $contributorsGroup = Get-AzADGroup -DisplayName "vsclk-core-contributors-3a5d"
    $contributorsGroup | Out-String | Write-Host -ForegroundColor DarkGray
    $contributorsGroup = $contributorsGroup.Id
    $readersGroup = Get-AzADGroup -DisplayName "vsclk-core-readers-fd84"
    $readersGroup | Out-String | Write-Host -ForegroundColor DarkGray
    $readersGroup = $readersGroup.Id
}
else {
    # Do not assign these group roles for production. They are JIT or break-glass only.
    $adminsGroup = ""
    $contributorsGroup = ""
    $readersGroup = ""
    $breakGlassGroup = Get-AzADGroup -DisplayName "BG-Codespaces"
    $breakGlassGroup | Out-String | Write-Host -ForegroundColor DarkGray
    $breakGlassGroup = $breakGlassGroup.Id
}

# Prepare the subscription onboarding ARM template and parameters
$armTemplate = (Get-Item "..\Templates\OnboardSubscription.Template.jsonc").FullName
$templateObject = Get-Content -Path $armTemplate -Raw | ConvertFrom-Json -AsHashtable
$parameters = @{
    prefix            = $Prefix
    env               = $Env
    component         = $Component
    plane             = $Plane
    location          = "westus2"
    opsSpId           = $opsSpId
    appSpId           = $appSpId
    firstPartyAppSpId = $firstPartyAppSpId
    adminsGroup       = $adminsGroup
    contributorsGroup = $contributorsGroup
    breakGlassGroup   = $breakGlassGroup
    readersGroup      = $readersGroup
    skipResources     = $DataType -or $Env -eq 'data'
}

# Execute the deployment
$deploymentName = "$($sub.Subscription.Name)-$((Get-Date).ToString('s').Replace(":","-").Replace(".","-"))"
Write-Host "Creating subscription deployment $deploymentName with parameters"
$parameters | Out-String | Write-Host -ForegroundColor DarkGray
$deployment = New-AzSubscriptionDeployment -Name $deploymentName -Location $parameters.location -TemplateObject $templateObject -TemplateParameterObject $parameters
$deployment | Out-String | Write-Host -ForegroundColor DarkGray

# Create the service principal certificate in the newly created key vault and assign the certificate to the service principal credentials
if ($opsSpId -and $Component -eq "core" -and $Plane -eq "ops") {
    "Creating core-ops service principal certificate" | Out-String | Write-Host
    $cert = . "$PSScriptRoot\Add-ServicePrincipalCert.ps1" -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane
    $cert | Out-String | Write-Host -ForegroundColor DarkGray
    "Assigning core-ops service principal certificate credentials" | Out-String | Write-Host
    $creds = . "$PSScriptRoot\Assign-ServicePrincipalCert.ps1" -Prefix $Prefix -Component $Component -Env $Env -Plane $Plane
    $creds | Out-String | Write-Host -ForegroundColor DarkGray
}

# Check for remaining User role assignments
$scope = "/subscriptions/$($sub.Subscription.Id)"
$userRoleAssignments = Get-AzRoleAssignment -Scope $scope | Where-Object -Property Scope -eq $scope | Where-Object -Property ObjectType -eq "User"

if ($userRoleAssignments) {
    Write-Warning "There are outstanding user role assignments in this subscription!"
    $userRoleAssignments | Out-String | Write-Host -ForegroundColor DarkGray
}
