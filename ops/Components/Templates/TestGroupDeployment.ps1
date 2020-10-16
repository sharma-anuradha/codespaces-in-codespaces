# TestGroupDeployment.ps1

#requires -version 7.0
#requires -modules @{ ModuleName = "Az.Accounts"; ModuleVersion="1.7.4" }
#requires -modules @{ ModuleName = "Az.Resources"; ModuleVersion="1.13.0" }

[CmdletBinding()]
param (
  [string]$SubscriptionName = '{{{subscriptionName}}}',
  [string]$SubscriptionId = '{{{subscriptionId}}}'
)

# Module dependencies
Import-Module "Az.Accounts" -Verbose:$false
Import-Module "Az.Resources" -Verbose:$false

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'

$script:Verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
  $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

function Select-Subscription() {
  $SubscriptionName = '{{{subscriptionName}}}'
  $SubscriptionId = '{{{subscriptionId}}}'

  if (!$SubscriptionName -or !$SubscriptionId) {
    if ('{{{plane}}}' -ne 'data') {
      Write-Warning "No subscription specified for $PSCommandPath"
    }
    return $null
  }

  Write-Verbose "Selecting azure subscription $SubscriptionName ($SubscriptionId)" -Verbose:$script:Verbose
  $sub = Select-AzSubscription -SubscriptionId $SubscriptionId
  $actualSubscriptionName = $sub.Subscription.Name
  if ($actualSubscriptionName -ne $SubscriptionName) {
    throw "Actual subscription name '$actualSubscriptionName' doesn't match expected name '$SubscriptionName'"
  }

  return $sub
}

function Get-TemplateFiles() {
  $targetFileName = [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.ScriptName)
  $baseFileName = '{{{baseFileName}}}'
  $baseRootIndex = $PSScriptRoot.IndexOf($baseFileName)

  if ($baseRootIndex -le 0) {
    throw "Can't find directory $baseFileName in $PSScriptRoot"
  }

  $baseRoot = Join-Path $PSScriptRoot.Substring(0, $baseRootIndex) $baseFileName
  Write-Verbose "Root folder: $baseRoot" -Verbose:$script:Verbose

  if (!(Test-Path -Path $baseRoot -PathType Container)) {
    throw "Base path does not exist: $baseRoot"
  }

  $templateFiles = Get-ChildItem -Path $baseRoot -Recurse -Filter $targetFileName
  Write-Verbose "Template Files:" -Verbose:$script:Verbose
  $templateFiles | Write-Verbose -Verbose:$script:Verbose

  if (!$templateFiles) {
    throw "Template file '$targetFileName' does not exist under '$baseRoot'"
  }

  $templateFiles
}

function Use-ResourceGroup([string]$ResourceGroup) {
  try {
    Get-AzResourcegroup -Name $ResourceGroup | Out-Null
  }
  catch {
    Write-Verbose "Creating resource group $ResourceGroup" -Verbose:$script:Verbose
    New-AzResourceGroup -Name $ResourceGroup -Location '{{{location}}}' | Out-Null
  }
}

function Test-ResourceGroupDeployment([string]$TemplateFile) {

  $subscriptionName = '{{{subscriptionName}}}'
  $ResourceGroup = '{{{baseName}}}'
  $templateFileName = Split-Path -Path $TemplateFile -Leaf
  Write-Host "Validating ARM template : $templateFileName ($subscriptionName/$resourceGroup)" -ForegroundColor Blue

  if (!(Test-Path -Path $TemplateFile -PathType Leaf)) {
    throw "Template file not found: $TemplateFile"
  }

  [string] $TemplateParametersFile = $null
  if ($templateFile.EndsWith('.arm.json')) {
    $TemplateParametersFile = $templateFile.Replace('.arm.json', '.arm.parameters.json')
    if (!(Test-Path $TemplateParametersFile)) {
      $TemplateParametersFile = $null
    }
  }

  Write-Verbose "TemplateFile: $TemplateFile" -Verbose:$script:Verbose
  Write-Verbose "TemplateParametersFile: $TemplateParametersFile" -Verbose:$script:Verbose
  Write-Verbose "Resource Group: $ResourceGroup" -Verbose:$script:Verbose

  # Execute validation in resource group
  Use-ResourceGroup $ResourceGroup
  $result = $null
  if ($TemplateParametersFile) {
    $result = Test-AzResourceGroupDeployment -ResourceGroupName $ResourceGroup -TemplateFile $TemplateFile -TemplateParameterFile $TemplateParametersFile -SkipTemplateParameterPrompt -Mode Incremental -Pre -Verbose:$script:Verbose
  }
  else {
    $result = Test-AzResourceGroupDeployment -ResourceGroupName $ResourceGroup -TemplateFile $TemplateFile -SkipTemplateParameterPrompt -Mode Incremental -Pre -Verbose:$script:Verbose
  }

  if ($result) {
    Write-Host "ARM validation Failed : $templateFileName : $($result.Code) : $($result.Message)" -ForegroundColor Red
    return $false
  }

  Write-Host "ARM validation succeeded : $templateFileName" -ForegroundColor Green
  return $true
}

function Main() {
  $templateFiles = Get-TemplateFiles

  if (Select-Subscription) {
    $errors = $false
    foreach ($templateFile in $templateFiles) {
      $ok = Test-ResourceGroupDeployment -TemplateFile $templateFile
      $errors = $errors -or !$ok
    }

    if ($errors) {
      throw "ARM validation failed"
    }
  }
}

Main
