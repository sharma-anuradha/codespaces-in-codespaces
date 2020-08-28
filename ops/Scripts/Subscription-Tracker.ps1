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
    [nullable[int]]$maxResourceGroupCount
    [nullable[int]]$ordinal
    [string]$plane
    [string]$premiumFilesInternalSettings
    [string]$region
    [nullable[ServiceType]]$serviceType
    [string]$storagePartitionedDnsFlag
    [System.Guid]$subscriptionId
    [string]$subscriptionName
    [string]$vnetInjection
}

# Generate Json data from Excel file
function Format-ExcelToJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExcelFile,
        [string]$JsonFile = "$PSScriptRoot\..\Components\subscriptions.json"
    )

    if ($ExcelFile -is "System.IO.FileSystemInfo") {
        $ExcelFile = $ExcelFile.FullName.ToString()
    }

    # Make sure the input file path is fully qualified
    $ExcelFile = [System.IO.Path]::GetFullPath($ExcelFile)
    Write-Verbose "Converting '$ExcelFile' to JSON"

    $dbConn = New-Object System.Data.OleDb.OleDbConnection

    try {
        $dbCmd = New-Object System.Data.OleDb.OleDbCommand
        $dbAdapter = New-Object System.Data.OleDb.OleDbDataAdapter
        $dbConn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=`"$ExcelFile`";Extended Properties=`"Excel 12.0 Xml;HDR=NO;TypeGuessRows=0;ImportMixedTypes=Text;IMEX=1`";"
        $dbConn.Open()

        $dbCmd.Connection = $dbConn
        $dbCmd.commandtext = "Select * from [Subscriptions$]"
        $dbAdapter.SelectCommand = $dbCmd
        $dataTable = New-Object System.Data.DataTable
        $rowsCount = $dbAdapter.Fill($dataTable)
        $columnsCount = $dataTable.Columns.Count

        function Find-HeaderColumn ([System.Data.DataRow]$row, [string]$header) {
            For ([int]$i = 0; $i -le $columnsCount; $i++) {
                if ($row[$i] -eq $header) {
                    return $i
                }
            }

            Write-Error "Unable to find Header:`'$header`'"
        }

        function Get-CellValue ([System.Data.DataRow]$row, [int]$col, [switch]$ToLower) {
            [string]$value = $row[$col]
            if ([string]::IsNullOrEmpty($value)) {
            $value = $null
            }

            if ($ToLower -and $value) {
            $value = $value.ToLowerInvariant()
            }

            return $value
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

        $subscriptions = [System.Collections.ArrayList]@()

        $foundHeader = $false
        For ([int]$i = 0; $i -le $rowsCount; $i++) {
            if (!$foundHeader) {
                if ($dataTable.Rows[$i][0] -ne "Environment") {
                    continue
                }

                # Find columns
                [int]$environmentColumn = 0
                [int]$subscriptionIdColumn = Find-HeaderColumn $dataTable.Rows[$i] "Subscription ID"
                [int]$subscriptionNameColumn = Find-HeaderColumn $dataTable.Rows[$i] "Subscription Name"
                [int]$subscriptionOldNameColumn = Find-HeaderColumn $dataTable.Rows[$i] "Old Name"
                [int]$enabledColumn = Find-HeaderColumn $dataTable.Rows[$i] "Enabled"
                [int]$maxResourceGroupCountColumn = Find-HeaderColumn $dataTable.Rows[$i] "Max RG Count"
                [int]$componentColumn = Find-HeaderColumn $dataTable.Rows[$i] "Component"
                [int]$planeColumn = Find-HeaderColumn $dataTable.Rows[$i] "Plane"
                [int]$premiumFilesInternalSettingsColumn = Find-HeaderColumn $dataTable.Rows[$i] "Storage 32gb Flag"
                [int]$storagePartitionedDnsFlagColumn = Find-HeaderColumn $dataTable.Rows[$i] "Storage Partitioned DNS Flag"
                [int]$serviceTypeColumn = Find-HeaderColumn $dataTable.Rows[$i] "Type"
                [int]$regionColumn = Find-HeaderColumn $dataTable.Rows[$i] "Region"
                [int]$ordinalColumn = Find-HeaderColumn $dataTable.Rows[$i] "Ordinal"
                [int]$generationColumn = Find-HeaderColumn $dataTable.Rows[$i] "Generation"
                [int]$ameColumn = Find-HeaderColumn $dataTable.Rows[$i] "AME"
                [int]$vnetInjectionColumn = Find-HeaderColumn $dataTable.Rows[$i] "VNet Injection"

                $foundHeader = $true
            }

            if ($dataTable.Rows[$i][0] -eq "Total") {
                break
            }

            $subscriptionName = Get-CellValue $dataTable.Rows[$i] $subscriptionNameColumn
            Write-Host "Processing row $i of ${rowsCount}: '$subscriptionName'" -ForegroundColor DarkGray

            # Skip blank subscription IDs
            $subscriptionIdValue = Get-CellValue $dataTable.Rows[$i] $subscriptionIdColumn
            [System.Guid]$subscriptionId = [System.Guid]::Empty

            if (!$subscriptionIdValue) {
                Write-Host "Skipping row ${i}: '$subscriptionName' has blank subscription id" -ForegroundColor Yellow
                continue
            }

            if (![System.Guid]::TryParse($subscriptionIdValue, [ref]$subscriptionId)) {
                Write-Host "Skipping row ${i}: '$subscriptionName' has invalid subscription id '$subscriptionIdValue'" -ForegroundColor Yellow
                continue
            }

            $subscription = [Subscription]::new()

            $subscription.subscriptionName = $subscriptionName

            # If Subscription has old name we should use it for now.
            $subscriptionOldName = Get-CellValue $dataTable.Rows[$i] $subscriptionOldNameColumn
            if ($null -ne $subscriptionOldName -and $subscriptionOldName.Trim().Length -ne 0) {
                $subscription.subscriptionName = $subscriptionOldName
            }

            $subscription.subscriptionId = $subscriptionId

            $enabledValue = Get-CellValue $dataTable.Rows[$i] $enabledColumn
            [bool]$enabled = $false
            if ([System.Boolean]::TryParse($enabledValue, [ref]$enabled)) {
                $subscription.enabled = $enabled
            }

            $subscription.environment = Get-CellValue $dataTable.Rows[$i] $environmentColumn -ToLower
            $subscription.component = Get-CellValue $dataTable.Rows[$i] $componentColumn -ToLower
            $subscription.plane = Get-CellValue $dataTable.Rows[$i] $planeColumn -ToLower
            $subscription.premiumFilesInternalSettings = Get-CellValue $dataTable.Rows[$i] $premiumFilesInternalSettingsColumn -ToLower
            $subscription.storagePartitionedDnsFlag = Get-CellValue $dataTable.Rows[$i] $storagePartitionedDnsFlagColumn -ToLower

            try {
                $subscription.serviceType = [ServiceType](Get-CellValue $dataTable.Rows[$i] $serviceTypeColumn)
            }
            catch {
                $subscription.serviceType = $null
            }

            $subscription.region = Get-CellValue $dataTable.Rows[$i] $regionColumn -ToLower
            $subscription.location = ConvertTo-AzureLocation $subscription.region

            $maxResourceGroupCountValue = Get-CellValue $dataTable.Rows[$i] $maxResourceGroupCountColumn
            [int]$maxResourceGroupCount = 0
            if ([System.Int32]::TryParse($maxResourceGroupCountValue, [ref]$maxResourceGroupCount)) {
                $subscription.maxResourceGroupCount = $maxResourceGroupCount
            }

            $ordinalValue = Get-CellValue $dataTable.Rows[$i] $ordinalColumn
            [int]$ordinal = 0
            if ([System.Int32]::TryParse($ordinalValue, [ref]$ordinal)) {
                $subscription.ordinal = $ordinal
            }

            $subscription.Generation = Get-CellValue $dataTable.Rows[$i] $generationColumn -ToLower
            $ameValue = Get-CellValue $dataTable.Rows[$i] $ameColumn

            [bool]$ame = $false
            if ([System.Boolean]::TryParse($ameValue, [ref]$ame)) {
                $subscription.ame = $ame
            }

            $subscription.vnetInjection = Get-CellValue $dataTable.Rows[$i] $vnetInjectionColumn -ToLower

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
    finally {
        if ($null -ne $dbConn -and $dbConn.State -eq [System.Data.ConnectionState]::Open) {
            $dbConn.Dispose()
            $dbConn.Close()
        }
    }
}

function Get-Subscriptions {
    [CmdletBinding(DefaultParameterSetName="default")]
    param(
        [Parameter(Position=0, ParameterSetName='default')]
        [Parameter(Position=0, ParameterSetName='SubscriptionId')]
        [Parameter(Position=0, ParameterSetName='SubscriptionName')]
        [string]$SubscriptionJsonFile = "$PSScriptRoot\..\Components\subscriptions.json",
        [Parameter(Position=1, ParameterSetName='SubscriptionId')]
        [System.Guid]$SubscriptionId = [System.Guid]::Empty,
        [Parameter(Position=2, ParameterSetName='SubscriptionName')]
        [string]$SubscriptionName
    )

    $subscriptions = Get-Content $SubscriptionJsonFile | Out-String | ConvertFrom-Json
    if ( [System.Guid]::Empty -ne $SubscriptionId) {
        $subscriptions = $subscriptions |  Where-Object { $_.subscriptionId -eq $SubscriptionId }
    }

    if ($SubscriptionName.Length -ne 0) {
        $subscriptions = $subscriptions |  Where-Object { $_.subscriptionName -like "$($SubscriptionName)*" }
    }

    $subscriptions | ForEach-Object {
        [Subscription]$subscription = [Subscription]$_
        Write-Output $subscription
    }
}

function Test-Airs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Subscription]$Subscription
    )

    Begin {
        $azureSubscriptions = az account list -o json | ConvertFrom-Json
    }

    Process {
        ForEach ($sub in $Subscription) {
            $found = $azureSubscriptions |  Where-Object { $_.id -eq $sub.subscriptionId }
            if (!$found) {
                Write-Error "Unable to find subscription:`'$($sub.subscriptionName)`' id:`'$($sub.subscriptionId)`'"
            } else {
                Write-Output $sub
            }
        }
    }
}

function Test-ResourceProviders {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Subscription]$Subscription
    )

    Process {
        ForEach ($sub in $Subscription) {
            $result = az account set --subscription $sub.subscriptionId
            if ($null -ne $result) {
                Write-Error "Invalid subscription:`'$($sub.subscriptionName)`' id:`'$($sub.subscriptionId)`'"
                continue
            }

            #$storageAccounts = az storage account list -o json | ConvertFrom-Json
            # Todo validate Storage Accounts
        }
    }
}

function Test-Rbac {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Subscription]$Subscription
    )

    Process {
        ForEach ($sub in $Subscription) {
            #TODO
        }
    }
}

# Validate azure against json
function Test-Azure {
    [CmdletBinding()]
    param(
        [string]$JsonFile = "$PSScriptRoot\..\Components\subscriptions.json"
    )

    #TODO - How should we from Azure validate all subscriptions are listed as expected on the json file?
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
        $_.serviceType -ne "storage" -or ($_.serviceType -eq "storage" -and $_.premiumFilesInternalSettings -eq "done" -and $_.storagePartitionedDnsFlag -eq "done")
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

    $subs = [ordered]@{}

    $jsonSubscriptions | ForEach-Object {
        $subscription = $_

        $azureSubscriptionSettings = [PSCustomObject]@{
            subscriptionId = $subscription.subscriptionId
            # Don't write out this property until the values in subscriptions.json have been verified
            # enabled = $subscription.enabled
        }
        if ($subscription.location) {
            $azureSubscriptionSettings | Add-Member -MemberType NoteProperty -Name "locations" -Value @($subscription.location)
        }
        if ($subscription.serviceType) {
            $azureSubscriptionSettings | Add-Member -MemberType NoteProperty -Name "serviceType" -Value ([string]$subscription.serviceType).ToLowerInvariant()
        }
        if ($null -ne $subscription.maxResourceGroupCount) {
            $azureSubscriptionSettings | Add-Member -MemberType NoteProperty -Name "maxResourceGroupCount" -Value $subscription.maxResourceGroupCount
        }

        $subs.Add($subscription.subscriptionName, $azureSubscriptionSettings)
    }

    $appSettings = [PSCustomObject]@{
        dataPlaneSettings = [PSCustomObject]@{
            subscriptions = $subs
        }
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
