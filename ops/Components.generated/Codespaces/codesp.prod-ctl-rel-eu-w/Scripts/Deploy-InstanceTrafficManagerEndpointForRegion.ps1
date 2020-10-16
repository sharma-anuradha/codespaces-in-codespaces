# Auto-Generated From Template
# "Deploy-InstanceTrafficManagerEndpointForRegion.ps1"
# with names file "codesp.prod-ctl-rel-eu-w.names.json"
# Do not edit this generated file. Edit the source file and rerun the generator instead.

# Deploy-InstanceTrafficManagerEndpointForRegion

# Resource names
[CmdletBinding()]
param (
    # For local testing, to run the cmdlet as the logged-in user
    [switch] $noLogin
)

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
    $VerbosePreference = $script:Verbose
    $PSDefaultParameterValues['*:Verbose'] = $script:Verbose
}
Import-Module Az.Accounts -Verbose:$false | Out-Null
Import-Module Az.Resources -Verbose:$false | Out-Null
Import-Module Az.TrafficManager -Verbose:$false | Out-Null

function Write-Heading($inputObject) {
    $inputObject | Out-String | Write-Host -ForegroundColor Green
}

function Write-Info($inputObject) {
    $inputObject | Out-String | Write-Host -ForegroundColor DarkGray
}

function Write-InfoJson($inputObject) {
    $inputObject | ConvertTo-Json -Depth 10 -EnumsAsStrings | Out-String | Write-Host -ForegroundColor DarkGray
}

function Write-VerboseInfo($inputObject) {
    $inputObject | Out-String | Write-Verbose -Verbose:$script:verbose
}

# Constants
$clusterIpAddressName = "vscs-codesp-prod-rel-eu-w-cluster-v1-ip"
$clusterResourceGroup = "vscs-codesp-prod-rel-eu-w"
$endpointLocation = "WestEurope"
$endpointName = $endpointLocation.ToLowerInvariant()
$endpointType = "AzureEndpoints"
$geoMappings = "GEO-EU GEO-ME GEO-AF GEO-AN" -split ' '
$profileName = "vscs-codesp-prod-rel-tm"
$profileResourceGroup = "vscs-codesp-prod-rel"
$subscriptionId = "babc8408-303e-4acc-9d13-194c075c1cce"
$clusterIpAddressId = "/subscriptions/$subscriptionId/resourceGroups/$clusterResourceGroup/providers/Microsoft.Network/publicIPAddresses/$clusterIpAddressName"
Write-VerboseInfo "clusterIpAddressId: $clusterIpAddressId"
Write-VerboseInfo "clusterIpAddressName: $clusterIpAddressName"
Write-VerboseInfo "clusterResourceGroup: $clusterResourceGroup"
Write-VerboseInfo "endpointLocation: $endpointLocation"
Write-VerboseInfo "endpointName: $endpointName"
Write-VerboseInfo "geoMappings: $geoMappings"
Write-VerboseInfo "profileName: $profileName"
Write-VerboseInfo "profileResourceGroup: $profileResourceGroup"
Write-VerboseInfo "subscriptionId: $subscriptionId"

# BEGIN
Write-Heading "Updating instance traffic manager endpoint $profileName/$endpointName in $profileResourceGroup"

# Login and Subscription Context
if (!$noLogin) {
    # TODO: add retry logic here due to container-instance bug
    Write-Info "Logging in with managed identity"
    Connect-AzAccount -Identity
}
Write-Info "Setting subscription context to $subscriptionId"
Set-AzContext -Subscription $subscriptionId | Out-Null

# Ensure that the profile exists
Write-Info "Getting traffic manager profile $profileName in $profileResourceGroup"
try {
    Get-AzTrafficManagerProfile `
        -ResourceGroupName $profileResourceGroup `
        -Name $profileName `
        -Verbose:$script:verbose | Out-Null
}
catch {
    Write-Info $_
    throw "Traffic manager profile $profileName not found in $profileResourceGroup"
}

# Get or create the endpoint
try {
    Write-Info "Getting traffic manager endpoint $profileName/$endpointName"
    $tmEndpoint = Get-AzTrafficManagerEndpoint `
        -ResourceGroupName $profileResourceGroup `
        -ProfileName $profileName `
        -Name $endpointName `
        -Type $endpointType
}
catch {
    Write-Info "Creating traffic manager endpoint $profileName/$endpointName in $profileResourceGroup"
    $tmEndpoint = New-AzTrafficManagerEndpoint `
        -GeoMapping $geoMappings `
        -EndpointLocation $endpointLocation `
        -EndpointStatus Enabled `
        -Name $endpointName `
        -ProfileName $profileName `
        -ResourceGroupName $profileResourceGroup `
        -TargetResourceId $clusterIpAddressId `
        -Type $endpointType
}

# Dump original values
Write-InfoJson $tmEndpoint

# Update the endpoint
Write-Info "Setting traffic manager endpoint $profileName/$endpointName"
$tmEndpoint.GeoMapping = $geoMappings
$tmEndpoint.TargetResourceId = $clusterIpAddressId
$tmEndpoint = Set-AzTrafficManagerEndpoint -TrafficManagerEndpoint $tmEndpoint

# Dump updated values
Write-Heading "Success"
Write-InfoJson $tmEndpoint
