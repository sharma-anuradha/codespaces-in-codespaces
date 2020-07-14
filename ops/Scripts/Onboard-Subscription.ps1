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
    Write-Host
    Write-Host "Registering resource providers and features..."
    # Register Resource Providers
    # TODO: how to make these specific to Component and Plane?
    (
        "Microsoft.Batch",
        "Microsoft.Compute",
        "Microsoft.ContainerRegistry",
        "Microsoft.DocumentDB",
        "Microsoft.Keyvault",
        "Microsoft.Kubernetes",
        "Microsoft.ManagedIdentity",
        "Microsoft.Maps",
        "Microsoft.Network",
        "Microsoft.Relay",
        "Microsoft.ServiceBus",
        "Microsoft.SignalRService",
        "Microsoft.Storage",
        "Microsoft.VirtualMachineImages"
    ) | ForEach-Object { 
        Write-Host $_ -ForegroundColor DarkGray
        Register-AzResourceProvider -ProviderNamespace $_ | Out-Null
    }

    # Enable Partitioned DNS to get the new quota limit of 5k storage accounts per subscription per region.
    # See https://microsoft.sharepoint.com/teams/AzureStorage/SitePages/Partitioned-DNS.aspx#how-can-you-enable-partitioned-dns-for-your-subscription-1
    "Microsoft.Storage/PartitionedData" | Write-Host -ForegroundColor DarkGray
    Register-AzProviderFeature -FeatureName "PartitionedDns" -ProviderNamespace "Microsoft.Storage" | Out-Null
}

# Get or create the ops service principal, needed for subscription RBAC.
# Except for data plane -- the ops sp should not have access to customer data.
$opsSpId = ""
if ($Plane -ne "data") {
    Write-Host
    Write-Host "Ensuring ops service principal exists..."
    $opsSp = Get-ServicePrincipal -Prefix $Prefix -Component "core" -Env $Env -Plane "ops" -Create
    $opsSp | Out-String | Write-Host -ForegroundColor DarkGray
    $opsSpId = $opsSp.Id
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
    # Do not assign these roles for production. They are JIT or break-glass only.
    $adminsGroup = ""
    $breakGlassGroup = Get-AzADGroup -DisplayName "BG-Codespaces"
    $breakGlassGroup | Out-String | Write-Host -ForegroundColor DarkGray
    $breakGlassGroup = $breakGlassGroup.Id
    $contributorsGroup = ""
    $readersGroup = ""
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
