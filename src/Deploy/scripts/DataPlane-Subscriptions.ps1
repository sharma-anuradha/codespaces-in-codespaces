<#
 .SYNOPSIS
    Manage Cloud Environment data-plane subscriptions and appsettings configuration.

 .DESCRIPTION
    Validate subscription configuration for one or more appsettings files.
    Validate that a subscription exists and has appropriate configuration and quotas.
    Onboard a new subscription with appropriate providers enabled and quotas.

 .PARAMETER appsettings

#>

#Requires -Version 5.1
#Requires -Modules @{ ModuleName="AzureRm"; ModuleVersion="6.13.1" }

[CmdletBinding(DefaultParameterSetName = "None")]
param (
    [Parameter(ParameterSetName = "Validate")]
    [switch]$validate,
    [Parameter(ParameterSetName = "Validate")]
    [switch]$register,
    [Parameter(ParameterSetName = "Validate")]
    [string]$appSettingsFilePattern,
    [switch]$interactive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'

# Default parameter values
$scripts = (Get-Item $PSScriptRoot).FullName
if (!$appSettingsFilePattern) {
    $appSettingsFilePattern = "${scripts}\..\..\Settings\appsettings*json"
}

# Promote switch to boolean
$script:loginInteractive = $false
if ($interactive) {
    $script:loginInteractive = $true
}

class ValidationError {
    [string]$FileName
    [object]$Message
    [bool]$IsError
} 

[ValidationError[]]$script:validationErrors = @()

class QuotaInfo {
    [string]$QuotaName
    [string]$DisplayName
    [int]$AzureLimit
    [int]$RequiredLimit
}

function Get-PropertyOrNull($obj, [string]$name, [bool]$required = $false) {

    if (!$obj)
    {
        if ($required) {
            throw "Parmeter obj is required"
        }
        return $null;
    }

    try {
        return $obj.$name
    }
    catch {
        if ($required) {
            throw "Property $name is rquired"
        }
        return $null
    }
}

function Write-Error-Or-Warning([string]$filename, [string]$message, [bool]$isError) {

    $validationError = [ValidationError]::new()
    $validationError.FileName = $filename
    $validationError.Message = $message
    $validationError.IsError = $isError
    $script:validationErrors += $validationError

    if ($isError) {
        Write-Host "ERROR: $message" -ForegroundColor Red
    }
    else {
        Write-Warning $message
    }
}

function ValidateAppSettingsFile([string]$filename) {
    Write-Host "Processing $filename"

    try {
        $content = Get-Content -Path $filename | Out-String
        $json = ConvertFrom-Json $content
    }
    catch {
        Write-Error $_.Exception.Message
        return
    }

    $appSettings = Get-PropertyOrNull $json "AppSettings"
    $dataPlaneSettings = Get-PropertyOrNull $appSettings "dataPlaneSettings"
    $defaultQuotas = Get-PropertyOrNull $dataPlaneSettings "defaultQuotas"
    $subscriptions = Get-PropertyOrNull $dataPlaneSettings "subscriptions"

    if (!$subscriptions) {
        return
    }
    
    $subscriptions.PSObject.Properties | ForEach-Object {
        $name = $_.Name
        $value = $_.Value
        $subscriptionName = $name
        $subscriptionSettings = $value
        Write-Host $subscriptionName
        Write-Host $subscriptionSettings

        # Read the azuresubscription properties
        $subscriptionId = Get-PropertyOrNull $subscriptionSettings "subscriptionId" -required $true
        $subscriptionName = $name
        $subscriptionEnabled = Get-PropertyOrNull $subscriptionSettings "enabled" -required $false
        if ($null -eq $subscriptionEnabled) {
            $subscriptionEnabled = $true
        }
        $requiredQuotas = Get-PropertyOrNull $subscriptionSettings "requiredQuotas" -required $false
        $color = "Green"
        if (!$subscriptionEnabled) {
            $color = "Yellow"
        }

        # Set the current subscription
        Set-AzureRmContext -Subscription $subscriptionId | Out-Null
        $azureRmSubscription = Get-AzureRmSubscription -SubscriptionId $subscriptionId
        if (!$azureRmSubscription) {
            throw "Could not get azure subscription info for $subscriptionId"
        }

        # Test the subscription id (this should always succeed!)
        if ($subscriptionId -ne $azureRmSubscription.Id) {
            Write-Error-Or-Warning $filename "Subscription doesn't match. Expected $subscriptionId, actual $($azureRmSubscription.Id)" $subscriptionEnabled
        }

        # Test the subscription id name
        if ($subscriptionName -ne $azureRmSubscription.Name) {
            Write-Error-Or-Warning $filename "Configured display name doesn't match. Expected $subscriptionName, actual $($azureRmSubscription.Name)" $subscriptionEnabled
        }

        $locations = Get-PropertyOrNull $subscriptionSettings "locations"
        $locations | ForEach-Object {
            $location = $_

            Write-Host
            Write-Host "$subscriptionName | $subscriptionId | $location | enabled:$($subscriptionEnabled)" -ForegroundColor $color

            # Load all providers and test for required providers
            $providers = Get-AzureRmResourceProvider -location $location -ListAvailable
            "Microsoft.Compute", "Microsoft.Storage", "Microsoft.Network" | ForEach-Object {
                $providerNamespace = $_
                $provider = $providers | Where-Object -Property ProviderNamespace -EQ $providerNamespace | Select-Object -First 1
                $registrationState = $provider.RegistrationState
                switch ($registrationState) {
                    "NotRegistered" {
                        if ($register) {
                            Write-Host "Registering resource provider $providerNamespace..."
                            Register-AzureRmResourceProvider -ProviderNamespace $providerNamespace | Out-Null
                        }
                        else {
                            Write-Error-Or-Warning $filename "Resource provider $providerNamespace is not registered" $subscriptionEnabled
                        }
                    }
                    "Registering" {
                        Write-Error-Or-Warning $filename "Resource provider $providerNamespace is sill registering." $false
                    }
                }
            }

            # Collect quota data
            if ($requiredQuotas) {
                $compute = Get-PropertyOrNull $requiredQuotas "compute"
                $storage = Get-PropertyOrNull $requiredQuotas "storage"
                $network = Get-PropertyOrNull $requiredQuotas "network"

                $quotas = @()

                # Compute quotas
                if ($compute) {
                    $vmUsage = Get-AzureRmVmUsage -location $location
                    $compute.PSObject.Properties | ForEach-Object {
                        $name = $_.Name
                        $value = Get-PropertyOrNull $compute $name
                        $quota = $vmUsage | Where-Object { $_.Name.Value -eq $name } | Select-Object -First 1
                        if ($quota) {
                            $quotaInfo = [QuotaInfo]::new()
                            $quotaInfo.QuotaName = $quota.Name.Value
                            $quotaInfo.DisplayName = $quota.Name.LocalizedValue
                            $quotaInfo.AzureLimit = $quota.Limit
                            $quotaInfo.RequiredLimit = $value
                            $quotas += $quotaInfo    
                        } else {
                            Write-Error-Or-Warning filename "Compute quota '$name' is not defined" $subscriptionEnabled
                        }
                    }
                } else {
                    Write-Error-Or-Warning $filename "No compute quotas specified" $subscriptionEnabled
                }

                # Storage quotas
                if ($storage) {
                    $value = Get-PropertyOrNull $storage "StorageAccounts"
                    $storageUsage = Get-AzureRmStorageUsage -Location $location
                    $quotaInfo = [QuotaInfo]::new()
                    $quotaInfo.QuotaName = $storageUsage.Name
                    $quotaInfo.DisplayName = $storageUsage.LocalizedName
                    $quotaInfo.AzureLimit = $storageUsage.Limit
                    $quotaInfo.RequiredLimit = $value
                    $quotas += $quotaInfo    
                } else {
                    Write-Error-Or-Warning $filename "No storage quotas specified" $subscriptionEnabled
                }

                # Network quotas
                if ($network) {
                    $networkUsage = Get-AzureRmNetworkUsage -Location $location
                    $network.PSObject.Properties | ForEach-Object {
                        $name = $_.Name
                        $value = Get-PropertyOrNull $network $name
                        $quota = $networkUsage | Where-Object { $_.Name.Value -eq $name } | Select-Object -First 1
                        if ($quota) {
                            $quotaInfo = [QuotaInfo]::new()
                            $quotaInfo.QuotaName = $quota.Name.Value
                            $quotaInfo.DisplayName = $quota.Name.LocalizedValue
                            $quotaInfo.AzureLimit = $quota.Limit
                            $quotaInfo.RequiredLimit = $value
                            $quotas += $quotaInfo    
                        } else {
                            Write-Error-Or-Warning $filename "Network quote '$name' is not defined" $subscriptionEnabled
                        }
                    }
                } else {
                    Write-Error-Or-Warning $filename "No network quotas specified" $subscriptionEnabled
                }

                # Display the quotas
                if (@($quotas).Length -gt 0) {
                    $quotas | Format-Table | Out-String | Write-Host

                    # Emit errors or warnings
                    $quotas | ForEach-Object {
                        $quota = $_
                        if ($quota.AzureLimit -lt $quota.RequiredLimit) {
                            Write-Error-Or-Warning $filename "Quota for '$($quota.QuotaName)' does not meet required limit of $($quota.RequiredLimit), $subscriptionName, $location" $subscriptionEnabled
                        }
                    }
                }

            } else {
                Write-Error-Or-Warning $filename "No quotas specified" $subscriptionEnabled
            }
        }
    }

    Write-Host
}

function ValidateAppSettings() {
    $items = Get-item $appsettingsFilePattern
 
    $script:validationErrors = @()
    $items | ForEach-Object { 
        $filename = $_.FullName
        ValidateAppSettingsFile $filename    
    }

    $warnings = $script:validationErrors | Where-Object { !$_.IsError }
    $warningCount = @($warnings).Length
    $warnings | ForEach-Object {
        Write-Host "WARNING: $($_.FileName) : $($_.Message)" -ForegroundColor Yellow
    }

    $errors = $script:validationErrors | Where-Object { $_.IsError }
    $errorCount = @($errors).Length
    $errors | ForEach-Object {
        Write-Host "ERROR: $($_.FileName) : $($_.Message)" -ForegroundColor Red
    }

    Write-Host
    Write-Host "Warnings: $($warningCount)"
    Write-Host "Errors: $($errorCount)"

    return $errorCount
}

function Main() {
    if ($validate) {
        ValidateAppSettings
    }
}

try {
    $errorCount = Main
    Exit $errorCount
} catch {
    $_.Exception | Out-String | Write-Host -ForegroundColor Red
    $_.ScriptStackTrace | Out-String | Write-Host -ForegroundColor Red
    Exit -1
}
