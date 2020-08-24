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
    [nullable[int]]$ordinal
    [string]$plane
    [string]$region
    [System.Guid]$subscriptionID
    [string]$subscriptionName
    [nullable[ServiceType]]$serviceType
}

class AzureSubscriptionSettings {
    [System.Guid]$subscriptionID
    [bool]$enabled
    [System.Collections.ArrayList]$locations = [System.Collections.ArrayList]::new()
    $servicePrincipal
    $quotas
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

    $excel = $null
    $wb = $null
    $sheet = $null

    try {
        $excel = New-Object -ComObject Excel.Application
        $excel.DisplayAlerts = $false

        $wb = $excel.Workbooks.Open($ExcelFile)
        if (-not $wb) {
            Write-Error "Unable to open Excel file."
            return
        }

        $sheet = $wb.worksheets | Where-Object { $_.name -eq "Subscriptions" }
        if (-not $sheet) {
            Write-Error "Unable to find Subscriptions sheet."
            return
        }

        function Find-HeaderRow () {
            $cell = $sheet.Cells.Find("Subscription ID")
            if (-not $cell) {
                Write-Error "Unable to find Header row."
                return -1
            }

            [int]$row = $cell.Row
            Disconnect-ComObject $cell
            return $row
        }

        function Find-Row ([string] $colName) {
            $cell = $sheet.Cells.Find($colName)
            if (-not $cell) {
                Write-Error "Unable to find $colName row."
                return -1
            }

            [int]$row = $cell.Row
            Disconnect-ComObject $cell
            return $row
        }

        function Find-HeaderColumn ([int]$headerRow, [string]$headerName) {

            $entireRow = $sheet.Cells.Item($headerRow, 1).EntireRow
            $cell = $entireRow.Find($headerName, [Type]::Missing, [Type]::Missing, 1)
            Disconnect-ComObject $entireRow

            if (-not $cell) {
                Write-Error "Unable to find $headerName column."
                return -1
            }

            [int]$column = $cell.Column
            Disconnect-ComObject $cell
            return $column
        }

        function Get-CellValue ([int]$row, [int]$col, [switch]$ToLower) {
            try {
              $cell = $sheet.Cells.Item($row, $col)

              [string]$value = $cell.Value2
              if ([string]::IsNullOrEmpty($value)) {
                $value = $null
              }

              if ($ToLower -and $value) {
                $value = $value.ToLowerInvariant()
              }

              return $value
            }
            finally {
              Disconnect-ComObject $cell
            }
        }

        function ConvertTo-AzureLocation([string]$Region) {
            switch ($Region) {
              "ap-se" { return "southeastasia" }
              "eu-w" { return "westeurope" }
              "us-e" { return "eastus" }
              "us-w2" { return "westus2" }
              "global" { return "" }
              "" { return "" }
            }

            throw "Unsupported region: $Region"
          }

        # Find first row
        [int]$headerRow = Find-HeaderRow
        if (-1 -eq $headerRow) {
            Write-Error "Unable to find Header row."
            return
        }

        [int]$firstRow = $headerRow + 1

        # Find last row
        [int]$totalRow = Find-Row "Total"
        if (-1 -eq $totalRow) {
            Write-Error "Unable to find Total row."
            return
        }

        [int]$lastRow = $totalRow - 1

        # Find columns
        [int]$subscriptionIDColumn = Find-HeaderColumn $headerRow "Subscription ID"
        [int]$subscriptionNameColumn = Find-HeaderColumn $headerRow "Subscription Name"
        [int]$enabledColumn = Find-HeaderColumn $headerRow "Enabled"
        [int]$environmentColumn = Find-HeaderColumn $headerRow "Environment"
        [int]$componentColumn = Find-HeaderColumn $headerRow "Component"
        [int]$planeColumn = Find-HeaderColumn $headerRow "Plane"
        [int]$serviceTypeColumn = Find-HeaderColumn $headerRow "Type"
        [int]$regionColumn = Find-HeaderColumn $headerRow "Region"
        [int]$ordinalColumn = Find-HeaderColumn $headerRow "Ordinal"
        [int]$generationColumn = Find-HeaderColumn $headerRow "Generation"
        [int]$ameColumn = Find-HeaderColumn $headerRow "AME"

        $subscriptions = [System.Collections.ArrayList]@()

        For ([int]$i = $firstRow; $i -le $lastRow; $i++) {

            $subscriptionName = Get-CellValue $i $subscriptionNameColumn
            Write-Host "Processing row $i of ${lastRow}: '$subscriptionName'" -ForegroundColor DarkGray

            # Skip blank subscription IDs
            $subscriptionIDValue = Get-CellValue $i $subscriptionIDColumn
            [System.Guid]$subscriptionId = [System.Guid]::Empty

            if (!$subscriptionIDValue) {
              Write-Host "Skipping row ${i}: '$subscriptionName' has blank subscription id" -ForegroundColor Yellow
              continue
            }

            if (![System.Guid]::TryParse($subscriptionIDValue, [ref]$subscriptionId)) {
              Write-Host "Skipping row ${i}: '$subscriptionName' has invalid subscription id '$subscriptionIDValue'" -ForegroundColor Yellow
              continue
            }

            $subscription = [Subscription]::new()

            $subscription.subscriptionName = $subscriptionName
            $subscription.subscriptionID = $subscriptionId

            [bool]$enabled = $false
            if ([System.Boolean]::TryParse($enabledColumn, [ref]$enabled)) {
                $subscription.enabled = $enabled
            }

            $subscription.environment = Get-CellValue $i $environmentColumn -ToLower
            $subscription.component = Get-CellValue $i $componentColumn -ToLower
            $subscription.plane = Get-CellValue $i $planeColumn -ToLower

            try {
                $subscription.serviceType = [ServiceType](Get-CellValue $i $serviceTypeColumn)
            }
            catch {
                $subscription.serviceType = $null
            }

            $subscription.region = Get-CellValue $i $regionColumn -ToLower
            $subscription.location = ConvertTo-AzureLocation $subscription.region

            $ordinalValue = Get-CellValue $i $ordinalColumn

            [int]$ordinal = 0
            if ([System.Int32]::TryParse($ordinalValue, [ref]$ordinal)) {
              $subscription.ordinal = $ordinal
            }

            $subscription.Generation = Get-CellValue $i $generationColumn -ToLower
            $ameValue = Get-CellValue $i $ameColumn

            [bool]$ame = $false
            if ([System.Boolean]::TryParse($ameValue, [ref]$ame)) {
              $subscription.ame = $ame
            }

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
        # Close the Workbook
        if ($excel) {
            $excel.Workbooks.Close()
        }

        # Release COM objects
        Disconnect-ComObject $sheet
        Disconnect-ComObject $wb

        # Quit Excel
        if ($excel) {
            $excel.Quit()
            Disconnect-ComObject $excel
        }
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
            if ($azureSubscription.Name -eq $jsonSubscription.subscriptionName -and $azureSubscription.SubscriptionId -eq $jsonSubscription.subscriptionID) {
                $script:found = $true
            }
        }

        if ($false -eq $found) {
            Write-Host "$($jsonSubscription.subscriptionName) {$($jsonSubscription.subscriptionID)} not found on Azure"
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

            if ($jsonSubscription.subscriptionName -eq $azureSubscription.Name -and $jsonSubscription.SubscriptionId -eq $subscription.subscriptionID) {
                $script:found = $true
            }
        }

        if ($false -eq $found) {
            Write-Host "$($azureSubscription.Name) {$($azureSubscription.subscriptionID)} not found on Json"
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
        $_.environment -eq $Environment -and $_.plane -eq "data" -and $null -ne $_.serviceType -and $_.generation -eq "v2-hybrid"
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
        $azureSubscriptionSettings.subscriptionID = $subscription.subscriptionID
        $azureSubscriptionSettings.enabled = $subscription.enabled
        $azureSubscriptionSettings.locations.Add($subscription.location) | Out-Null
        $azureSubscriptionSettings.serviceType = ([string]$subscription.serviceType).ToLowerInvariant()

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

    [string]$env = $Environment
    if ($Environment -eq "prod" -or $Environment -eq "ppe") {
        if ($Canary) {
            $env += "-can"
        } else {
            $env += "-rel"
        }
    }

    $header = "// GENERATED BY A TOOL. DO NOT EDIT" + [System.Environment]::NewLine
    $file = "appsettings.subscriptions.$env.jsonc"
    $appSettings | ConvertTo-Json -Depth 50 | ForEach-Object { $header + $_ } | Out-File -Encoding utf8 -FilePath $(Join-Path $OutputPath $file)
}
