#requires -version 7.0

# Global error handling -- ensure non-zero exit code on failure
trap {
    Write-Error $_
    exit 1
}

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:Verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
    $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}