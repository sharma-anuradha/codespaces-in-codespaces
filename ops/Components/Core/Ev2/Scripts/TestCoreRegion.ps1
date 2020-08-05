#requires -version 7.0

#requires -modules @{ ModuleName = "Az.Resources"; ModuleVersion="2.3.0" }
#requires -modules @{ ModuleName = "Az.ManagedServiceIdentity"; ModuleVersion="0.7.3" }

# Module dependencies
Import-Module "Az.Resources" -Verbose:$false

# Global error handling
trap {
    Write-Error $_
    exit 1
}

# Preamble
$scriptFolder = (Get-Item $PSCommandPath).DirectoryName
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

# TODO: Validate each region?
$resourceGroupName = 'vscs-core-test-ctl-ci-us-w2'

$templateFilePath = Join-Path $scriptFolder .. ARM 'Core.dev-ctl-ci-us-w2.Template.jsonc'

Select-AzSubscription 'vscs-core-test'

$parameters = @{
}

Test-AzResourceGroupDeployment `
    -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath `
    -TemplateParameterObject $parameters
