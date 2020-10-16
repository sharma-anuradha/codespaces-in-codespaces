# Auto-Generated From Template
# "codesp.environment.subscription.rbac.arm.json.ps1"
# with names file "codesp.prod-data.names.json"
# from template file "NewAzureSubscriptionDeployment.ps1"
# Do not edit this generated file. Edit the source file and rerun the generator instead.

# NewAzureSubscriptionDeployment.ps1

#requires -version 7.0
#requires -modules @{ ModuleName = "Az.Accounts"; ModuleVersion="1.7.4" }
#requires -modules @{ ModuleName = "Az.Resources"; ModuleVersion="1.13.0" }

[CmdletBinding()]
param(
    [string]$SubscriptionId = "undefined",
    [string]$armTemplate,
    [string]$location = "WestUs2",
    [string]$plane = "data",
    [switch]$SkipRegistrations
)

# Module dependencies
Import-Module "Az.Accounts" -Verbose:$false
Import-Module "Az.Resources" -Verbose:$false

# Preamble
Set-StrictMode -Version Latest
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}


function Select-AzureSubscription() {
    if (-not $SubscriptionId) {
        throw "-SubscriptionId is required"
    }

    Select-AzSubscription -SubscriptionId $SubscriptionId -Verbose:$script:verbose
}

function Get-ArmTemplatePath() {
    $path = $armTemplate

    if (!$path) {
        $path = (Get-Item -Path $PSCommandPath.Remove('.ps1')).FullName
    }

    if (!(Test-Path -Path $path -PathType Leaf)) {
        throw "ARM template file does not exist: $path"
    }
    Write-Verbose "TemplateFile: $path" -Verbose:$script:verbose
    $path
}

function Invoke-SubscriptionDeployment([string]$subscriptionName, [string]$armTemplatePath) {
    $fileName = Split-Path -Path $armTemplatePath -Leaf
    $deploymentName = "$SubscriptionName-$((Get-Date).ToString('s').Replace(":","-").Replace(".","-"))"

    Write-Host "Invoking ARM template '$fileName' in subscription '$subscriptionName' ($Location)" -ForegroundColor Green
    Write-Verbose "Creating subscription deployment $deploymentName" -Verbse:$script:verbose

    $deployment = New-AzSubscriptionDeployment -Name $deploymentName -Location $Location -TemplateFile $armTemplatePath -Verbose:$script:verbose
    $deployment | ConvertTo-Json -Depth 10 -EnumsAsStrings | Out-String | Write-Host -ForegroundColor DarkGray
}

function Main() {
    $sub = Select-AzureSubscription
    $armTemplatePath = Get-ArmTemplatePath
    Invoke-SubscriptionDeployment -subscriptionName $sub.Subscription.Name -armTemplatePath $armTemplatePath
}

Main
