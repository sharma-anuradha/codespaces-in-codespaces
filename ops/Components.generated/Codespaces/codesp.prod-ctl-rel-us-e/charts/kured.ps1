# Auto-Generated From Template
# "kured.ps1"
# with names file "codesp.prod-ctl-rel-us-e.names.json"
# from template file "HelmInstall.ps1"
# Do not edit this generated file. Edit the source file and rerun the generator instead.

# HelmInstall.ps1

#requires -version 7.0
#requires -modules @{ ModuleName = "Az.Accounts"; ModuleVersion="1.7.4" }
#requires -modules @{ ModuleName = "Az.Resources"; ModuleVersion="1.13.0" }
#requires -modules @{ ModuleName = "Az.Aks"; ModuleVersion="1.2.0" }

[CmdletBinding()]
param (
  [string]$SubscriptionName = 'vscs-codesp-prod-ctl',
  [string]$SubscriptionId = 'babc8408-303e-4acc-9d13-194c075c1cce'
)

# Module dependencies
Import-Module "Az.Accounts" -Verbose:$false
Import-Module "Az.Aks" -Verbose:$false
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
  if (!$SubscriptionName -or !$SubscriptionId) {
    if ('ctl' -ne 'data') {
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

function Get-ChartFolder() {
  $chartFolderPath = $PSCommandPath.Replace('.ps1','')

  if (!(Test-Path -Path $chartFolderPath -PathType Container)) {
    throw "Chart folder not found: '$chartFolderPath'"
  }

  $chartFolderPath
}

function Test-HelmExe() {
  try {
    $helm = Get-Command 'helm.exe'
    $helm | Out-String | Write-Verbose -Verbose:$script:Verbose
  }
  catch {
    throw "Helm.exe is not found"
  }
}


function Main() {
  $chartFolder = Get-ChartFolder
  $chartName = Split-Path $chartFolder -Leaf
  Write-Verbose "Chart name: $chartName" -Verbose:$script:Verbose
  Write-Verbose "Chart path: $chartFolder" -Verbose:$script:Verbose

  Test-HelmExe

  if (Select-Subscription) {
    Write-Host "Getting credentials for 'vscs-codesp-prod-rel-us-e-cluster-v1'" -ForegroundColor DarkGray
    Import-AzAksCredential -ResourceGroupName 'vscs-codesp-prod-rel-us-e' -Name 'vscs-codesp-prod-rel-us-e-cluster-v1' -Force -Verbose:$script:Verbose

    Write-Host "Installing chart '$chartName' to 'vscs-codesp-prod-rel-us-e-cluster-v1' in namespace 'kube-system'" -ForegroundColor green
    & helm.exe show all $chartFolder | Out-String | Write-Host -ForegroundColor DarkGray
    & helm.exe upgrade "${chartName}-release" "$chartFolder" --namespace "kube-system" --install | Out-String | Write-Host -ForegroundColor DarkGray
  }
}

Main
