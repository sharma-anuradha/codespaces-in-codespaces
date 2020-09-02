# Onboard-LegacySubscription.ps1

#requires -version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$SubscriptionNames,
    [switch]$SkipRegistrations,
    [switch]$SkipTags,
    [switch]$SkipRbac,
    [switch]$AdminOwner
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

class SubscriptionInfo {
    [string]$SubscriptionName
    [string]$Prefix
    [string]$Component
    [string]$Environment
    [string]$Plane
    [string]$Type
    [string]$Region
    [nullable[int]]$Ordinal
    [bool]$IsLegacy

    SubscriptionInfo([string]$SubscriptionName) {
        $this.SubscriptionName = $SubscriptionName
        $parts = $SubscriptionName.Split('-');
        $upper = $parts.GetUpperBound(0)
        if ($upper -ge 0) {
            $this.Prefix = $parts[0]
            $this.IsLegacy = $this.Prefix.StartsWith('vsclk-')
            if ($upper -ge 1) {
                $this.Component = $parts[1]
                if ($upper -ge 2) {
                    $this.Environment = $parts[2]
                    if ($upper -ge 3) {
                        $this.Plane = $parts[3]
                        if ($upper -ge 4) {
                            $this.Type = $parts[4]
                            if ($upper -ge 6) {
                                $geo = $parts[5]
                                $regionCode = $parts[6]
                                $this.Region = "$geo-$regionCode"
                                if ($upper -ge 7) {
                                    $this.Ordinal = [int]$parts[7]
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

function Get-AdSp([string]$DisplayName) {
    $sp = Get-AzADServicePrincipal -DisplayName $DisplayName
    if (!$sp) {
        throw "Service principal does not exist: $DisplayName"
    }
    "$($sp.DisplayName) $($sp.Id)" | Out-String | Write-Host -ForegroundColor DarkGray
    $sp
}

function Get-AdSpByAppId([string]$ApplicationId) {
    $sp = Get-AzADServicePrincipal -ApplicationId $ApplicationId
    if (!$sp) {
        throw "Service principal does not exist for appid: $ApplicationId"
    }
    "$($sp.DisplayName) $($sp.Id)" | Out-String | Write-Host -ForegroundColor DarkGray
    $sp
}


function Get-AdGroup([string]$DisplayName) {
    $group = Get-AzADGroup -DisplayName $DisplayName
    if (!$group) {
        throw "Group does not exist: $DisplayName"
    }
    "$($group.DisplayName) $($group.Id)" | Out-String | Write-Host -ForegroundColor DarkGray
    $group
}

function Invoke-OnboardLegacySubscription([string]$SubscriptionName) {

    $subscriptionInfo = New-Object SubscriptionInfo -ArgumentList $subscriptionName

    # Validate that the subscription exists
    try {
        $sub = Select-AzSubscription -Subscription $SubscriptionName
    }
    catch {
        throw "Legacy subscription not found: ${SubscriptionName}: $_"
    }
    $env = $subscriptionInfo.Environment
    Write-Host "Onboarding legacy subscription '$($sub.Subscription.Name)' for environment '$env'" -ForegroundColor Green

    # Provider Registrations
    if (!$SkipRegistrations) {
        $PartitionedData = !($subscriptionInfo.IsLegacy)
        Register-DefaultProvidersAndFeatures -PartitionedData $PartitionedData
    }

    # Subscription Tags
    if (!$SkipTags) {
        Write-Host "Updating subscription tags" -ForegroundColor Green
        $tags = @{
            prefix = $subscriptionInfo.Prefix ?? ""
            component = $subscriptionInfo.Component ?? ""
            env = $subscriptionInfo.Environment ?? ""
            plane = $subscriptionInfo.Plane ?? ""
            type = $subscriptionInfo.Type ?? ""
            region = $subscriptionInfo.Region ?? ""
            ordinal = [string]($subscriptionInfo.Ordinal) ?? ""
            serviceTreeUrl = 'https://servicetree.msftcloudes.com/main.html#/ServiceModel/Home/8fa58105-2fc7-4ffb-8d9e-5654c301864b'
            serviceTreeId = '8fa58105-2fc7-4ffb-8d9e-5654c301864b'
        }
        Update-AzTag -ResourceId "/subscriptions/$($sub.Subscription.Id)" -Tag $tags -Operation Merge | Out-String | Write-Host -ForegroundColor DarkGray
    }

    # Assign RBAC
    if (!$SkipRbac) {
        Write-Host "Assigning break-glass access control" -ForegroundColor Green
        New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $breakGlassGroup | Out-Null

        Write-Host "Assigning service principal access control" -ForegroundColor Green
        $appSp = Get-AdSp -DisplayName "vsclk-online-$env-app-sp"
        New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $appSp | Out-Null

        if (!($subscriptionInfo.Plane -eq 'data')) {
            $opsSp = Get-AdSp -DisplayName "vsclk-online-$env-devops-sp"
            New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $opsSp | Out-Null
        }

        if ($env -eq "dev") {
            Write-Host "Assigning team access control for dev" -ForegroundColor Green
            New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $adminsGroup | Out-Null
            New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $contributorsGroup | Out-Null
            New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $readersGroup | Out-Null

            if ($subscriptionInfo.Plane -eq 'data' -and $subscriptionInfo.Type -eq 'compute') {
                Write-Host "Assigning first-party app access for dev" -ForegroundColor Green
                New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $firstPartyAppDev | Out-Null
            }
        }
        elseif ($env -eq "ppe") {
            Write-Host "Assigning team access control for ppe" -ForegroundColor Green
            if ($AdminOwner) {
                New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $adminsGroup | Out-Null
            }
            New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $adminsGroup | Out-Null
            New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $contributorsGroup | Out-Null
            New-SubscriptionRoleAssignment -RoleDefinitionName "Reader" -Assignee $readersGroup | Out-Null

            if ($subscriptionInfo.Plane -eq 'data' -and $subscriptionInfo.Type -eq 'compute') {
                Write-Host "Assigning first-party app access for ppe" -ForegroundColor Green
                New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $firstPartyAppPpe | Out-Null
            }
        }
        elseif ($env -eq "prod") {
            Write-Host "Assigning team access control for prod" -ForegroundColor Green
            if ($AdminOwner) {
                New-SubscriptionRoleAssignment -RoleDefinitionName "Owner" -Assignee $adminsGroup | Out-Null
            }

            if ($subscriptionInfo.Plane -eq 'data' -and $subscriptionInfo.Type -eq 'compute') {
                Write-Host "Assigning first-party app access for prod" -ForegroundColor Green
                New-SubscriptionRoleAssignment -RoleDefinitionName "Contributor" -Assignee $firstPartyAppProd | Out-Null
            }
        }
    }
}

# User must be Owner to run this script on the subscription.
# Assert-SignedInUserIsOwner | Out-Null

# Get the group accounts and appids for subscription RBAC.
if (!$SkipRbac) {
    Write-Host "Getting group security accounts..." -ForegroundColor Green
    $script:adminsGroup = Get-AdGroup -DisplayName "vsclk-core-admin-a98a"
    $script:breakGlassGroup = Get-AdGroup -DisplayName "vsclk-core-breakglass-823b"
    $script:contributorsGroup = Get-AdGroup -DisplayName "vsclk-core-contributors-3a5d"
    $script:readersGroup = Get-AdGroup -DisplayName "vsclk-core-readers-fd84"
    Write-Host "Getting first-party appids..." -ForegroundColor Green
    $script:firstPartyAppDev = Get-AdSpByAppId -ApplicationId "48ef7923-268f-473d-bcf1-07f0997961f4"
    $script:firstPartyAppProd = Get-AdSpByAppId -ApplicationId "9bd5ab7f-4031-4045-ace9-6bebbad202f6"
    $script:firstPartyAppPpe = $script:firstPartyAppProd
}

foreach ($SubscripionName in $SubscriptionNames) {
    Invoke-OnboardLegacySubscription -SubscriptionName $SubscripionName
}
