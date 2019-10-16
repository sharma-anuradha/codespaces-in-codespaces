# Custom script extension script that is executed on Windows VMs.
# This script will live in the vsclk-cluster repo, but checking it in here for now for reference.

param 
(
    # Blob URL of the VM agent. Contains SAS token.
    [string] $vmAgentBlobUrl, 
    # Base 64 encoded (UTF8) json blob. No nested json as this is parsed into a hashtable<string, string>
    [string] $base64ParametersBlob,
    [string] $serviceVersionNumber = "1",
    [string] $vsoAgentRootDir = "c:\vsonline\vsoagent",
    [string] $logFile = "c:\windows-vm-init.txt"
)

function Log([string] $message)
{
    Write-Host $message
    $message = "[$(Get-Date -Format o)] $message $([Environment]::NewLine)"
    [System.IO.File]::AppendAllText($logFile, $message)
}

$ErrorActionPreference = "Stop"

try
{
    Log "Starting script."
    Log "VM agent blob url: $vmAgentBlobUrl"
    Log "Downloading the VM agent to '$vsoAgentRootDir\bin'"

    # Download the VM agent.
    if (-Not (Test-Path $vsoAgentRootDir))
    {
        New-Item $vsoAgentRootDir -ItemType Directory 
        Set-Location $vsoAgentRootDir
        Log "Download the VSO agent..."
        Invoke-WebRequest -Uri $vmAgentBlobUrl -OutFile "vsoagent.zip"
        Expand-Archive -Path "vsoagent.zip" -DestinationPath "bin"
        Remove-Item "vsoagent.zip" -Force
    }

    # Execute bulk of init logic that was downloaded with the VM agent.
    & $vsoAgentRootDir\bin\VMAgent\EnvironmentAgent\Scripts\WindowsInit.ps1 -vsoAgentDir "$vsoAgentRootDir\bin" -base64ParametersBlob "$base64ParametersBlob" -logFile "$logFile"

    Log "Finished windows-vm-init.ps1."
}
catch 
{
    Log "There was an error during script execution:"
    Log $_
    throw
}