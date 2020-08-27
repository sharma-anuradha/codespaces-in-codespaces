# Subscription-Tracker.ps1
# Tracks subscriptions information.

#  The Azure service type supported by the subscription.
enum ServiceType {
    # Azure Compute
    Compute = 0

    # Azure Networking
    Network = 1

    # Azure Storage
    Storage = 2

    # Azure KeyVault.
    KeyVault = 3
}

class Subscription {
    [bool]$ame
    [string]$component
    [bool]$enabled
    [string]$environment
    [string]$generation
    [string]$location
    # [nullable[int]]$maxResourceGroupCount
    [nullable[int]]$ordinal
    [string]$plane
    [string]$premiumFilesInternalSettings
    [string]$region
    [nullable[ServiceType]]$serviceType
    [string]$storagePartionedDnsFlag
    [System.Guid]$subscriptionId
    [string]$subscriptionName
    [string]$vnetInjection
}

class AzureSubscriptionSettings {
    [System.Guid]$subscriptionId
    # [bool]$enabled
    [System.Collections.ArrayList]$locations = [System.Collections.ArrayList]::new()
    $servicePrincipal
    $quotas
    # [nullable[int]]$maxResourceGroupCount
    $serviceType
}

class AppSettings {
    [DataPlaneSettings]$dataPlaneSettings = [DataPlaneSettings]::new()
}

class DataPlaneSettings {
    $subscriptions = [ordered]@{}
}

function Disconnect-ComObject($Object) {
  if ($Object) {
    while ([System.Runtime.Interopservices.Marshal]::ReleaseComObject($Object)) {}
  }
}

function ConvertTo-AzureLocation([string]$Region) {
    switch ($Region) {
        "ap-se" { return "southeastasia" }
        "eu-w" { return "westeurope" }
        "us-e" { return "eastus" }
        "us-e2euap" { return "eastus2euap" }
        "us-w2" { return "westus2" }
        "global" { return "" }
        "" { return "" }
    }

    throw "Unsupported region: $Region"
}

# Generate Json data from Csv file
function Format-CsvToJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsvFile,
        [string]$JsonFile = "$PSScriptRoot\..\Components\subscriptions.json"
    )

    if ($CsvFile -is "System.IO.FileSystemInfo") {
        $CsvFile = $CsvFile.FullName.ToString()
    }

    # Make sure the input file path is fully qualified
    $CsvFile = [System.IO.Path]::GetFullPath($CsvFile)
    Write-Verbose "Converting '$CsvFile' to JSON"

    function Get-CsvValue($obj, [string]$propertyName, [switch]$ToLower) {
        $value = $obj.$propertyName
        if ($value) {
            if($ToLower) {
                $value = $value.ToLowerInvariant()
            }
            $value = $value.Trim()
        }
        return $value
    }

    $subscriptions = [System.Collections.ArrayList]@()

    Import-Csv -Path $CsvFile | ForEach-Object {
        $subscriptionName = Get-CsvValue $_ 'Subscription Name'
        Write-Host "Processing row $i of ${lastRow}: '$subscriptionName'" -ForegroundColor DarkGray

        # Skip blank subscription IDs
        $subscriptionIdValue = Get-CsvValue $_ 'Subscription ID'
        [System.Guid]$subscriptionId = [System.Guid]::Empty

        if (!$subscriptionIdValue) {
            Write-Host "Skipping row ${i}: '$subscriptionName' has blank subscription id" -ForegroundColor Yellow
            return
        }

        if (![System.Guid]::TryParse($subscriptionIdValue, [ref]$subscriptionId)) {
            Write-Host "Skipping row ${i}: '$subscriptionName' has invalid subscription id '$subscriptionIdValue'" -ForegroundColor Yellow
            return
        }

        $subscription = [Subscription]::new()

        $subscription.subscriptionName = $subscriptionName

        # If Subscription has old name we should use it for now.
        $subscriptionOldName = Get-CsvValue $_ 'Old Name '
        if ($null -ne $subscriptionOldName -and $subscriptionOldName.Trim().Length -ne 0) {
            $subscription.subscriptionName = $subscriptionOldName
        }

        $subscription.subscriptionId = $subscriptionId

        $enabledValue = Get-CsvValue $_ 'Enabled'
        [bool]$enabled = $false
        if ([System.Boolean]::TryParse($enabledValue, [ref]$enabled)) {
            $subscription.enabled = $enabled
        }

        $subscription.environment = Get-CsvValue $_ 'Environment' -ToLower
        $subscription.component = Get-CsvValue $_ 'Component' -ToLower
        $subscription.plane = Get-CsvValue $_ 'Plane' -ToLower
        $subscription.premiumFilesInternalSettings = Get-CsvValue $_ 'Storage 32gb Flag ' -ToLower
        $subscription.storagePartionedDnsFlag = Get-CsvValue $_ 'Storage Partitioned DNS Flag ' -ToLower

        try {
            $subscription.serviceType = [ServiceType](Get-CsvValue $_ 'Type')
        }
        catch {
            $subscription.serviceType = $null
        }

        $subscription.region = Get-CsvValue $_ 'Region' -ToLower
        $subscription.location = ConvertTo-AzureLocation $subscription.region

        # $maxResourceGroupCountValue = Get-CsvValue $_ 'Max RG Count'
        # [int]$maxResourceGroupCount = 0
        # if ([System.Int32]::TryParse($maxResourceGroupCountValue, [ref]$maxResourceGroupCount)) {
        #     $subscription.maxResourceGroupCount = $maxResourceGroupCount
        # }

        $ordinalValue = Get-CsvValue $_ 'Ordinal'
        [int]$ordinal = 0
        if ([System.Int32]::TryParse($ordinalValue, [ref]$ordinal)) {
            $subscription.ordinal = $ordinal
        }

        $subscription.Generation = Get-CsvValue $_ 'Generation' -ToLower

        $ameValue = Get-CsvValue $_ 'AME'
        [bool]$ame = $false
        if ([System.Boolean]::TryParse($ameValue, [ref]$ame)) {
            $subscription.ame = $ame
        }

        $subscription.vnetInjection = Get-CsvValue $_ 'VNet Injection ' -ToLower

        $subscriptions.Add($subscription) | Out-Null
    }

    $subscriptions = $subscriptions | Sort-Object -Property subscriptionName

    if (-not $JsonFile) {
        Write-Host ($subscriptions | ConvertTo-Json -EnumsAsStrings)
    }
    else {
        $subscriptions | ConvertTo-Json -EnumsAsStrings | Out-File -Encoding:utf8 -FilePath:$JsonFile
    }
}

# Validate json against Azure
function Test-Json {
    [CmdletBinding()]
    param(
        [string]$JsonFile = "$PSScriptRoot\..\Components\subscriptions.json"
    )

    $jsonSubscriptions = Get-Content $JsonFile | Out-String | ConvertFrom-Json
    $azureSubscriptions = Get-AzSubscription | Where-Object { $_.Name -like 'vscs*' }

    $jsonSubscriptions | ForEach-Object {
        [Subscription]$jsonSubscription = $_

        $found = $false
        $azureSubscriptions | Where-Object {
            $azureSubscription = $_
            if ($azureSubscription.Name -eq $jsonSubscription.subscriptionName -and $azureSubscription.SubscriptionId -eq $jsonSubscription.subscriptionId) {
                $script:found = $true
            }
        }

        if ($false -eq $found) {
            Write-Host "$($jsonSubscription.subscriptionName) {$($jsonSubscription.subscriptionId)} not found on Azure"
        }
    }
}

# Validate azure against json
function Test-Azure {
    [CmdletBinding()]
    param(
        [string]$JsonFile = "$PSScriptRoot\..\Components\subscriptions.json"
    )

    $jsonSubscriptions = Get-Content $JsonFile | Out-String | ConvertFrom-Json
    $azureSubscriptions = Get-AzSubscription | Where-Object { $_.Name -like 'vscs*' }

    $azureSubscriptions | ForEach-Object {
        $azureSubscription = $_

        $found = $false
        $jsonSubscriptions | Where-Object {
            [Subscription]$jsonSubscription = $_

            if ($jsonSubscription.subscriptionName -eq $azureSubscription.Name -and $jsonSubscription.SubscriptionId -eq $subscription.subscriptionId) {
                $script:found = $true
            }
        }

        if ($false -eq $found) {
            Write-Host "$($azureSubscription.Name) {$($azureSubscription.subscriptionId)} not found on Json"
        }
    }
}

# update app.settings
function Build-AppSettings {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('dev','ppe','prod')]
        [string]$Environment,
        [string]$InputJsonFile = "$PSScriptRoot\..\Components\subscriptions.json",
        [Switch]$Canary = $false,
        [string]$OutputPath = "$PSScriptRoot\..\..\src\Settings"
    )

    $jsonSubscriptions = Get-Content $InputJsonFile | Out-String | ConvertFrom-Json

    $jsonSubscriptions = $jsonSubscriptions | Where-Object {
        $_.environment -eq $Environment -and $_.plane -eq "data" -and $_.generation -ne "v2"
    }

    $jsonSubscriptions = $jsonSubscriptions | Where-Object {
        $_.serviceType -ne "compute" -or ($_.serviceType -eq "compute" -and $_.vnetInjection -eq "done")
    }

    $jsonSubscriptions = $jsonSubscriptions | Where-Object {
        $_.serviceType -ne "storage" -or ($_.serviceType -eq "storage" -and $_.premiumFilesInternalSettings -eq "done" -and $_.storagePartionedDnsFlag -eq "done")
    }

    if ($Canary) {
        $jsonSubscriptions = $jsonSubscriptions | Where-Object { $_.ordinal -eq 1 }
     }

    if ($jsonSubscriptions.Count -eq 0) {
        Write-Host "No subscription found."
        return
    }

    $appSettings = [AppSettings]::new()
    $appSettings.dataPlaneSettings.subscriptions.Clear() | Out-Null

    $jsonSubscriptions | ForEach-Object {
        $subscription = $_

        $azureSubscriptionSettings = [AzureSubscriptionSettings]::new()
        $azureSubscriptionSettings.subscriptionId = $subscription.subscriptionId
        # Don't write out this property until the values in subscriptions.json have been verified
        # $azureSubscriptionSettings.enabled = $subscription.enabled
        if ($subscription.location -ne "") {
            $azureSubscriptionSettings.locations.Add($subscription.location) | Out-Null
        }
        $azureSubscriptionSettings.serviceType = ([string]$subscription.serviceType).ToLowerInvariant()
        # if ($null -ne $subscription.maxResourceGroupCount) {
        #     $azureSubscriptionSettings.maxResourceGroupCount = $subscription.maxResourceGroupCount
        # }

        $appSettings.dataPlaneSettings.subscriptions.Add($subscription.subscriptionName, $azureSubscriptionSettings)
    }

    if(-not $OutputPath){
        Write-Host ($appSettings | ConvertTo-Json -EnumsAsStrings -Depth 50)
        return
    }

    if(![System.IO.Directory]::Exists($OutputPath)){
        Write-Error "Path '$OutputPath' is invalid."
        return
    }

    [string]$env = $Environment.ToLowerInvariant()
    if ($env -eq "prod" -or $env -eq "ppe") {
        if ($Canary) {
            $env += "-can"
        } else {
            $env += "-rel"
        }
    }

    $header = "// Auto-Generated from script: 'Subscription-Tracker.ps1'" + [System.Environment]::NewLine +  " // DO NOT EDIT THIS FILE."    + [System.Environment]::NewLine
    $file = "appsettings.subscriptions.$env.jsonc"
    @{ AppSettings = $appSettings } | ConvertTo-Json -Depth 50 | ForEach-Object { $header + $_ } | Out-File -Encoding utf8 -FilePath $(Join-Path $OutputPath $file)
}
