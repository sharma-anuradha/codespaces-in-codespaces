# Auto-Generated From Template
# "Deploy-InstanceTrafficManagerProfile.ps1"
# with names file "codesp.prod-ctl-rel.names.json"
# Do not edit this generated file. Edit the source file and rerun the generator instead.

# Deploy-InstanceTrafficManagerProfile

# Resource names
[CmdletBinding()]
param (
    [string] $subscriptionId = "babc8408-303e-4acc-9d13-194c075c1cce",
    [string] $resourceGroup = "vscs-codesp-prod-rel",
    [string] $profileName = "vscs-codesp-prod-rel-tm",
    [string] $location = "WestUs2",
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
    $inputOutput | Out-String | Write-Verbose -Verbose:$script:verbose
}

# BEGIN
Write-Heading "Updating instance traffic manager profile $profileName"

# Constants
$dnsTtl = 300
$monitorIntervalInSeconds = 30
$monitorTimeoutInSeconds = 5
$monitorPath = "/healthz"
$monitorPort = 443
$monitorProtocol = "HTTPS"
$monitorToleratedNumberOfFailures = 3
$relativeDnsName = $profileName
$trafficRoutingMethod = "Geographic"

# Login and Subscription Context
if (!$noLogin) {
    Write-Info "Logging in with managed identity for subscription $subscriptionId"
    Connect-AzAccount -Identity
}
Set-AzContext -Subscription $subscriptionId | Out-Null

# Create traffic manager profile if it doesn't exist
Write-Info "Getting traffic manager profile $profileName"
try {
    $tmProfile = Get-AzTrafficManagerProfile -ResourceGroupName $resourceGroup -Name $profileName -Verbose:$script:verbose
}
catch {
    $tmProfile = $null
}
if (!$tmProfile) {
    $rg = Get-AzResourceGroup -Name $resourceGroup
    if (!$rg) {
        Write-Info "Creating resource group $resourceGroup"
        $rg = New-AzResourceGroup -Name $resourceGroup -Location $location -Verbose:$script:verbose
    }
    Write-Info "Creating traffic manager profile $profileName in $resourceGroup"
    $tmProfile = New-AzTrafficManagerProfile `
        -Name $profileName `
        -MonitorProtocol $monitorProtocol `
        -MonitorPort $monitorPort `
        -MonitorPath $monitorPath `
        -MonitorIntervalInSeconds $monitorIntervalInSeconds `
        -MonitorTimeoutInSeconds $monitorTimeoutInSeconds `
        -MonitorToleratedNumberOfFailures $monitorToleratedNumberOfFailures `
        -ResourceGroupName $resourceGroup `
        -RelativeDnsName $relativeDnsName `
        -TrafficRoutingMethod $trafficRoutingMethod `
        -Ttl $dnsTtl `
        -Verbose:$script:verbose
    Write-InfoJson $tmProfile
}

# Traffic Routing
$tmProfile.TrafficRoutingMethod = $trafficRoutingMethod

# DNS Config
$tmProfile.RelativeDnsName = $relativeDnsName
$tmProfile.Ttl = $dnsTtl

# Monitor Config
$tmProfile.MonitorProtocol = $monitorProtocol
$tmProfile.MonitorPort = $monitorPort
$tmProfile.MonitorPath = $monitorPath
$tmProfile.MonitorIntervalInSeconds = $monitorIntervalInSeconds
$tmProfile.MonitorTimeoutInSeconds = $monitorTimeoutInSeconds
$tmProfile.MonitorToleratedNumberOfFailures = $monitorToleratedNumberOfFailures

# Update the traffic manager profile. Note that the profile object maintains all existing endpoints.
Write-Info "Updating traffic manager profile $profileName"
Write-InfoJson $tmProfile
$tmProfile = Set-AzTrafficManagerProfile -TrafficManagerProfile $tmProfile -Verbose:$script:verbose
Write-Heading "Success"
Write-InfoJson $tmProfile
