# Subscription-Tracker.ps1
# Tracks subscriptions information.

#requires -version 7.0

Set-StrictMode -Version Latest

. "$PSScriptRoot\OpsUtilities.ps1"

# Define here the number of instance environments.
[hashtable]$instanceEnvironments = [ordered]@{ dev = 2; ppe = 2; prod = 1}

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
    [nullable[bool]]$dbEnabled
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
    [string]$subscriptionNewName
    [string]$subscriptionOldName
    [string]$vnetInjection
    [SubscriptionValidation]$validation = [SubscriptionValidation]::new()

    [PSCustomObject]SelectValidationResults() {
        $selected = [PSCustomObject]@{
            name = $this.subscriptionName
            id = $this.subscriptionId
            isValid = $this.validation.IsValid()
        }

        foreach ($property in $this.validation.PSObject.Properties) {
            $result = $property.Value
            $resultStr = "$($result.result)"
            if ($result.message) {
                $resultStr += " - $($result.message)"
            }
            $selected | Add-Member -MemberType NoteProperty -Name $property.Name -Value $resultStr
        }

        return $selected
    }
}

enum ValidationResultType {
    Untested
    Warning
    Error
    Success
}

class ValidationResult {
    [ValidationResultType]$result
    [string]$message
}

class SubscriptionValidation {
    [ValidationResult]$airs = [ValidationResult]::new()
    [ValidationResult]$coreQuotas = [ValidationResult]::new()
    [ValidationResult]$partitionedDns = [ValidationResult]::new()
    [ValidationResult]$premiumFilesInternalSettings = [ValidationResult]::new()
    [ValidationResult]$rbac = [ValidationResult]::new()
    [ValidationResult]$resourceGroup = [ValidationResult]::new()
    [ValidationResult]$resourceProviders = [ValidationResult]::new()
    [ValidationResult]$subscriptionName = [ValidationResult]::new()
    [ValidationResult]$tags = [ValidationResult]::new()
    [ValidationResult]$vnetInjection = [ValidationResult]::new()

    [bool]IsValid() {
        foreach ($property in $this.PSObject.Properties) {
            if ($property.Value.result -eq [ValidationResultType]::Error) {
                return $false
            }
        }
        return $true
    }
}

function ConvertTo-AzureLocation {
    [CmdletBinding()]
    param(
        [string]$Region
    )

    switch ($Region) {
        "ap-se" { return "southeastasia" }
        "eu-w" { return "westeurope" }
        "us-e" { return "eastus" }
        "us-e2c" { return "eastus2euap" }
        "us-w2" { return "westus2" }
        "global" { return "" }
        "" { return "" }
    }

    throw "Unsupported region: $Region"
}

function ConvertTo-VsoUtilEnvironment {
    [CmdletBinding()]
    param(
        [string]$env
    )

    switch ($env.ToLowerInvariant()) {
        "dev" { return "Development" }
        "ppe" { return "Staging" }
        "prod" { return "Production" }
    }

    throw "Unsupported environment: $env"
}

function ConvertTo-AzureRegion {
    [CmdletBinding()]
    param(
        [string]$location
    )

    switch ($location) {
        "southeastasia" { return "ap-se" }
        "westeurope" { return "eu-w" }
        "eastus" { return "us-e" }
        "eastus2euap" { return "us-e2c" }
        "westus2" { return "us-w2" }
        "" { return "global" }
    }

    throw "Unsupported location: $location"
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
                [int]$subscriptionRenameDoneColumn = Find-HeaderColumn $dataTable.Rows[$i] "Rename Done"
                [int]$dbEnabledColumn = Find-HeaderColumn $dataTable.Rows[$i] "DB Enabled"
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

            $subscriptionOldName = Get-CellValue $dataTable.Rows[$i] $subscriptionOldNameColumn
            $subscription.subscriptionOldName = $subscriptionOldName

            # Don't set subscriptionNewName if there isn't an old name
            $subscription.subscriptionNewName = $subscription.subscriptionOldName ? $subscriptionName : ""

            $renameDoneValue = Get-CellValue $dataTable.Rows[$i] $subscriptionRenameDoneColumn
            [bool]$renameDone = $false
            [Diagnostics.CodeAnalysis.SuppressMessageAttribute('UseDeclaredVarsMoreThanAssignments', '', Justification='Capture to prevent output in script')]
            $_discard = [System.Boolean]::TryParse($renameDoneValue, [ref]$renameDone)

            # subscriptionName property should be set to what we believe is current in Azure
            if ($subscription.subscriptionOldName -and -not $renameDone) {
                $subscription.subscriptionName = $subscriptionOldName
            }

            $subscription.subscriptionId = $subscriptionId

            $dbEnabledValue = Get-CellValue $dataTable.Rows[$i] $dbEnabledColumn
            [bool]$dbEnabled = $null
            if ([System.Boolean]::TryParse($dbEnabledValue, [ref]$dbEnabled)) {
                $subscription.dbEnabled = $dbEnabled
            }

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

        $subscriptions = $subscriptions | Sort-Object -Property subscriptionName | Select-Object -ExcludeProperty validation

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
        [string]$SubscriptionName,
        [Parameter(Position=3, ParameterSetName='default')]
        [Parameter(Position=3, ParameterSetName='SubscriptionName')]
        [ValidateSet('dev','ppe','prod')]
        [string]$Environment,
        [Parameter(Position=4, ParameterSetName='default')]
        [Parameter(Position=4, ParameterSetName='SubscriptionName')]
        [string]$Plane,
        [Parameter(Position=5, ParameterSetName='default')]
        [Parameter(Position=5, ParameterSetName='SubscriptionName')]
        [string]$Generation,
        [Parameter(Position=6, ParameterSetName='default')]
        [Parameter(Position=6, ParameterSetName='SubscriptionName')]
        [Switch]$Canary = $false,
        [Switch]$UseAppSettingsFilters = $false
    )

    $subscriptions = Get-Content $SubscriptionJsonFile | Out-String | ConvertFrom-Json
    if ( [System.Guid]::Empty -ne $SubscriptionId) {
        $subscriptions = $subscriptions | Where-Object {
            $_.subscriptionId -eq $SubscriptionId
        }
    }

    if ($SubscriptionName.Length -ne 0) {
        $subscriptions = $subscriptions | Where-Object {
            $_.subscriptionName -like "$($SubscriptionName)"
        }
    }

    if ($Environment.Length -ne 0) {
        $subscriptions = $subscriptions | Where-Object {
            $_.environment -eq $Environment
        }
    }

    if ($Plane.Length -ne 0) {
        $subscriptions = $subscriptions | Where-Object {
            $_.plane -eq $Plane
        }
    }

    if ($Generation.Length -ne 0) {
        $subscriptions = $subscriptions | Where-Object {
            $_.generation -like "$($Generation)"
        }
    }

    if ($Canary) {
        $subscriptions = $subscriptions | Where-Object {
            $_.region -eq "us-e2c" -or $_.region -eq "global"
        }
     }

    if ($UseAppSettingsFilters) {
        $subscriptions = $subscriptions | Where-Object {
            $_.generation -ne "v2"
        }

        $subscriptions = $subscriptions | Where-Object {
            $_.serviceType -ne "compute" -or ($_.serviceType -eq "compute" -and $_.vnetInjection -eq "done")
        }

        $subscriptions = $subscriptions | Where-Object {
            $_.serviceType -ne "storage" -or ($_.serviceType -eq "storage" -and $_.premiumFilesInternalSettings -eq "done" -and $_.storagePartitionedDnsFlag -eq "done")
        }
    }

    $subscriptions | ForEach-Object {
        [Subscription]$subscription = [Subscription]$_
        Write-Output $subscription
    }
}

function Test-Any {
    [CmdletBinding()]
    param($EvaluateCondition,
        [Parameter(ValueFromPipeline = $true)] $ObjectToTest)
    begin {
        $any = $false
    }
    process {
        if (-not $any -and (& $EvaluateCondition $ObjectToTest)) {
            $any = $true
        }
    }
    end {
        $any
    }
}

function Test-All {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Process {
        ForEach ($sub in $Subscriptions) {
            $sub | Test-Airs | Test-SubscriptionName | Test-Tags | Test-ResourceGroup | Test-CoreQuotas | Test-ResourceProviders | Test-Rbac | Test-32GbFlags | Test-PartionedDns | Test-VnetInjection
        }
    }
}

function Test-Airs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Begin {
        $azureSubscriptions = az account list -o json | ConvertFrom-Json
    }

    Process {
        ForEach ($sub in $Subscriptions) {
            $found = $azureSubscriptions |  Where-Object {
                $_.id -eq $sub.subscriptionId
            }

            if (!$found) {
                $sub.validation.airs.result = [ValidationResultType]::Error
                $sub.validation.airs.message = "not found"
            } else {
                $sub.validation.airs.result = [ValidationResultType]::Success
                # NOTE: Generally we always pass the sub through the pipeline,
                #       but in the case where the account has no access to the
                #       sub there is no point. This is a gatekeeper for other
                #       validations.
                Write-Output $sub
            }
        }
    }
}

function Test-SubscriptionName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Begin {
        $azureSubscriptions = az account list -o json | ConvertFrom-Json
    }

    Process {
        ForEach ($sub in $Subscriptions) {
            $azSub = $azureSubscriptions |  Where-Object {
                $_.id -eq $sub.subscriptionId
            }

            $sub.validation.subscriptionName.result = [ValidationResultType]::Success

            if (!$azSub) {
                $sub.validation.subscriptionName.result = [ValidationResultType]::Error
                $sub.validation.subscriptionName.message = "not found"
            } else {
                if ($sub.subscriptionOldName -eq $azSub.name) {
                    $sub.validation.subscriptionName.result = [ValidationResultType]::Warning
                    $sub.validation.subscriptionName.message = "contains legacy name and needs to be renamed"
                } else {
                    if ($sub.subscriptionName  -ne $azSub.name) {
                        $sub.validation.subscriptionName.result = [ValidationResultType]::Error
                        $sub.validation.subscriptionName.message = "has incorrect name. Expected:`'$($sub.subscriptionName)`', Actual:`'$($azSub.name)`'"
                    }
                }

                Write-Output $sub
            }
        }
    }
}

function Test-Tags {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Process {
        ForEach ($sub in $Subscriptions) {
            $sub.validation.tags.result = [ValidationResultType]::Success

            $tags = az tag list --subscription $sub.subscriptionId -o json | ConvertFrom-Json

            $warningTagNames = "component", "env", "ordinal", "plane", "prefix", "region", "type", "serviceTreeId", "serviceTreeUrl"
            $expectedTagValues = "$($sub.component)", "$($sub.environment)", "$($sub.ordinal)", "$($sub.plane)", "vscs", "$(ConvertTo-AzureRegion $sub.location)", "$($sub.serviceType)", $null, $null

            # Validate all expected tags
            for ($i = 0; $i -lt $warningTagNames.Count; $i++) {
                $tagName = $warningTagNames[$i]
                $tag = $tags | Where-Object {
                    $_.tagName -eq $tagName
                }

                if (!$tag) {
                    $sub.validation.tags.result = [ValidationResultType]::Warning
                    $sub.validation.tags.message = "doesn't have Tag:`'$($tagName)`'"
                } else {
                    if ($null -ne $expectedTagValues[$i] -and $tag.values.tagValue -ne $expectedTagValues[$i]) {
                        $sub.validation.tags.result = [ValidationResultType]::Warning
                        $sub.validation.tags.message = "contains Tag:`'$($tagName)`' with invalid value. Expected:`'$( $expectedTagValues[$i])`', Actual:`'$($tag.values.tagValue)`'"
                    }
                }
            }

            Write-Output $sub
        }
    }
}

function Test-ResourceGroup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Process {
        ForEach ($sub in $Subscriptions) {
            $sub.validation.resourceGroup.result = [ValidationResultType]::Success

            $result = az account set --subscription $sub.subscriptionId
            if ($null -ne $result) {
                $sub.validation.resourceGroup.result = [ValidationResultType]::Error
                $sub.validation.resourceGroup.message = "Invalid subscription"
            } else {
                # Check if subscription has a limit of maximum resource groups
                if ($null -ne $sub.maxResourceGroupCount) {

                    # Some environments may have more than 1 instance, in this case we should take that in factor to calculate the maximum
                    $instances = $instanceEnvironments["$($sub.environment)"]
                    if ($null -eq $instances) {
                        $sub.validation.resourceGroup.result = [ValidationResultType]::Error
                        $sub.validation.resourceGroup.message = "Invalid environment $($sub.environment), unable to find number of instance environments."
                        Write-Output $sub
                        continue
                    }

                    $maximum = $sub.maxResourceGroupCount * $instances

                    # If we are over capacity we should error out requesting immediate attention and move resources out of the subscription if possible.
                    $resourceGroups = az group list --subscription $sub.subscriptionId -o json | ConvertFrom-Json
                    if ($resourceGroups.Count -gt $maximum) {
                        $sub.validation.resourceGroup.result = [ValidationResultType]::Error
                        $sub.validation.resourceGroup.message = "has more resource groups than allowed. Expected:`'$($maximum)`', Actual:`'$($resourceGroups.Count)`'"
                    }
                }

                Write-Output $sub
            }
        }
    }
}

function Test-CoreQuotas {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Process {
        ForEach ($sub in $Subscriptions) {
            $result = az account set --subscription $sub.subscriptionId
            if ($null -ne $result) {
                $sub.validation.coreQuotas.result = [ValidationResultType]::Error
                $sub.validation.coreQuotas.message = "Invalid subscription"
            } else {
                # Todo validate CoreQuotas
            }

            Write-Output $sub
        }
    }
}

function Test-ResourceProviders {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Begin {
        # TODO: Update this to reflect different defaults per resource type
        $defaultRPs = Get-DefaultResourceProviders
    }

    Process {
        ForEach ($sub in $Subscriptions) {
            $sub.validation.resourceProviders.result = [ValidationResultType]::Success

            if ($sub.generation -eq "v1-legacy") {
                Write-Output $sub
                continue
            }

            $result = az account set --subscription $sub.subscriptionId
            if ($null -ne $result) {
                $sub.validation.resourceProviders.result = [ValidationResultType]::Error
                $sub.validation.resourceProviders.message = "Invalid subscription"
            } else {
                $registeredRPs = az provider list --query "[].{Provider:namespace, Status:registrationState}" --out json | ConvertFrom-Json | Where-Object {$_.Status -eq "Registered"}

                ForEach ($provider in $defaultRPs) {
                    if (!($registeredRPs | Test-Any {$_.Provider -like $provider})) {
                        $sub.validation.resourceProviders.result = [ValidationResultType]::Error
                        $sub.validation.resourceProviders.message = "missing provider $provider"
                    }
                }
            }

            Write-Output $sub
        }
    }
}

function Test-Rbac {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Process {
        ForEach ($sub in $Subscriptions) {
            $sub.validation.rbac.result = [ValidationResultType]::Success
            $result = az account set --subscription $sub.subscriptionId
            if ($null -ne $result) {
                $sub.validation.rbac.result = [ValidationResultType]::Error
                $sub.validation.rbac.message = "Invalid subscription"
            } else {

                if ($sub.serviceType -eq [ServiceType]::Compute -or $sub.serviceType -eq [ServiceType]::Network) {
                    if ($sub.environment -eq "dev") {
                        # The Visual Studio Services API Dev first party appid.
                        # $appId = "48ef7923-268f-473d-bcf1-07f0997961f4";
                        $firstpartyapp = "a0b50ae5-6a18-4e3d-b553-f5c7d4e5b87e"
                    } else {
                        # The Visual Studio Services API first party appid.
                        # $appId = "9bd5ab7f-4031-4045-ace9-6bebbad202f6";
                        $firstpartyapp = "944cc140-b92f-4bce-a09a-426e827a040c"
                    }

                    # $firstpartyapp = az ad sp list --filter "appId eq '$appId'" --query [0].objectId -o tsv
                    if ($null -eq $firstpartyapp) {
                        $sub.validation.rbac.result = [ValidationResultType]::Error
                        $sub.validation.rbac.message = "Unable to find AppId: '$appId'"
                    } else {
                        $role = az role assignment list --role "Contributor" --assignee $firstpartyapp -o json | ConvertFrom-Json
                        if ($null -eq  $role) {
                            $sub.validation.rbac.result = [ValidationResultType]::Error
                            $sub.validation.rbac.message = "missing contributor role for first party app."
                        }
                    }
                }
            }

            Write-Output $sub
        }
    }
}

function Test-32GbFlags {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Begin {
        $featureFlag = "PremiumFilesInternalSettings"
    }

    Process {
        ForEach ($sub in $Subscriptions) {
            $sub.validation.premiumFilesInternalSettings.result = [ValidationResultType]::Success
            if (-not ($sub.premiumFilesInternalSettings -like "done")) {
                Write-Output $sub
                continue
            }

            $response = az feature show --namespace Microsoft.Storage -n $featureFlag --subscription $sub.subscriptionId | ConvertFrom-Json
            if (-not $response -or -not ($response.properties.state -like "registered")) {
                $sub.validation.premiumFilesInternalSettings.result = [ValidationResultType]::Error
                $sub.validation.premiumFilesInternalSettings.message = "missing feature flag $featureFlag"
            }

            Write-Output $sub
        }
    }
}

function Test-PartionedDns {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Begin {
        $featureFlag = "PartitionedDns"
    }

    Process {
        ForEach ($sub in $Subscriptions) {
            $sub.validation.partitionedDns.result = [ValidationResultType]::Success
            if (-not ($sub.storagePartitionedDnsFlag -like "done")) {
                Write-Output $sub
                continue
            }

            $response = az feature show --namespace Microsoft.Storage -n $featureFlag --subscription $sub.subscriptionId | ConvertFrom-Json
            if (-not $response -or -not ($response.properties.state -like "registered")) {
                $sub.validation.partitionedDns.result = [ValidationResultType]::Error
                $sub.validation.partitionedDns.message = "missing feature flag $featureFlag"
            }

            Write-Output $sub
        }
    }
}

function Test-VnetInjection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Process {
        ForEach ($sub in $Subscriptions) {
            # TODO: Not sure there is any cheap way to validate this
            Write-Output $sub
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
        [string]$OutputPath = "$PSScriptRoot\..\..\src\Settings",
        [Switch]$UpdateDatabse = $false
    )

    $subscriptions = Get-Subscriptions -SubscriptionJsonFile:$InputJsonFile -Environment:$Environment -Plane:"data" -Canary:$Canary -UseAppSettingsFilters:$true

    if ($subscriptions.Count -eq 0) {
        Write-Host "No subscription found."
        return
    }

    $subs = [ordered]@{}

    $subscriptions | ForEach-Object {
        $subscription = $_

        # For Canary, don't even list subs that aren't really allocated to it yet.
        # The AzureSubscriptionCatalog can get confused if it contains
        # conflicting info about the same subscription (legacy/enabled according
        # to prod-rel but network/disabled according to prod-can, for example).
        if ($Canary -and ($subscription.subscriptionOldName -ne "vso-prod-data-001-00")) {
            return
        }

        $azureSubscriptionSettings = [PSCustomObject]@{
            subscriptionId = $subscription.subscriptionId
        }

        # "Disabled" v1-hybrid subs should continue to be used in a legacy fashion (type-agnostic)
        $treatAsLegacy = ($subscription.generation -eq 'v1-hybrid') -and (!$subscription.enabled)

        if (!$treatAsLegacy) {
            if (!$subscription.enabled) {
                $azureSubscriptionSettings | Add-Member -MemberType NoteProperty -Name "enabled" -Value $subscription.enabled
            }
            if ($subscription.location) {
                $azureSubscriptionSettings | Add-Member -MemberType NoteProperty -Name "locations" -Value @($subscription.location)
            }
            if ($null -ne $subscription.serviceType) {
                $azureSubscriptionSettings | Add-Member -MemberType NoteProperty -Name "serviceType" -Value ([string]$subscription.serviceType).ToLowerInvariant()
            }
            if ($null -ne $subscription.maxResourceGroupCount) {
                $azureSubscriptionSettings | Add-Member -MemberType NoteProperty -Name "maxResourceGroupCount" -Value $subscription.maxResourceGroupCount
            }
        }

        $subs.Add($subscription.subscriptionName, $azureSubscriptionSettings)

        if ($UpdateDatabse) {
            Enable-Subscription Subscription:$subscription
        }
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

function Enable-Subscription {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions,
        [string]$VsoUtilDirectory = $(Join-Path $PSScriptRoot .. .. bin debug VsoUtil)
    )

    Process {
        ForEach ($sub in $Subscriptions) {
            Write-Host
            Write-Host "Processing $($sub.subscriptionName) ($($sub.subscriptionId)), with dbEnabled=$($sub.dbEnabled ? $sub.dbEnabled : "null"), location=$($sub.location)"

            $options = ""
            if ($null -eq $sub.dbEnabled) {
                $options = "--remove"
            } else {
                if ($false -eq $sub.dbEnabled) {
                    $options = "--disable"
                }
            }

            $env = ConvertTo-VsoUtilEnvironment -env $sub.environment

            Push-Location $VsoUtilDirectory
            dotnet VsoUtil.dll enable-subscription --verbose --location $sub.location --env $env --subscription $sub.subscriptionId $options
            Pop-Location
        }
    }
}

function Rename-Subscription {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [Object[]]$Subscriptions
    )

    Begin {
        $extensionInfo = az extension show --name account | ConvertFrom-Json
        if (-not $extensionInfo) {
            Write-Host "Renaming subscriptions requires the 'account' extension"
            Write-Host "You can install with:"
            Write-Host "  az extension add --name account"
            Write-Error "Try again after installing"
        }
    }

    Process {
        ForEach ($sub in $Subscriptions) {
            Write-Host
            Write-Host "Renaming $($sub.subscriptionOldName) ($($sub.subscriptionId)) to $($sub.subscriptionNewName)"

            az account subscription rename --id $sub.subscriptionId --name $sub.subscriptionNewName
        }
    }
}
