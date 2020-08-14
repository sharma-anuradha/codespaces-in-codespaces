# This is a dummy script that dumps environment variables to the host console.

#requires -version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'

function Write-Command([string]$Command) {
    try {
        $cmd = Get-Command $Command
        $cmd | Out-String | Write-Host -ForegroundColor DarkGray
    }
    catch {
        Write-Host "Command not found: $_" -ForegroundColor Red
        Write-Host
    }
}

"Environment Variables" | Write-Host -ForegroundColor Green
Get-ChildItem env:* | Sort-Object Name | Out-String | Write-Host -ForegroundColor Green

"Installed Tools" | Write-Host -ForegroundColor Green

Write-command -Command az
Write-command -Command dotnet
Write-Command -Command helm
Write-Command -Command jq
Write-Command -Command kubectl
Write-Command -Command openssl
Write-Command -Command pwsh
Write-Command -Command yq

"Installed Modules" | Write-Host -ForegroundColor Green
Import-Module -Name Az
Get-Module | Select-Object -Property Name, Version | Sort-Object -Property Name | Out-String | Write-Host -ForegroundColor DarkGray
