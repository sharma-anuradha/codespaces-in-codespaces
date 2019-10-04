# Custom script extension script that is executed on Windows VMs.
# This script is read out of a storage account during custom script execution and is not automatically updated (TODO).
# Dev location: vsodevciusw2vmusw2 under the windows-vm-init container.

param 
(
    [string] $vmAgentBlobUrl, # Blob URL contains SAS token, no need for access key.
    [string] $vmAgentInputQueueName,
    [string] $vmAgentInputQueueUrl,
    [string] $vmAgentInputQueueSasToken,
    [string] $username,
    [string] $password
)

function Log ([string] $message)
{
    $message = "[$(Get-Date -Format o)] $message $([Environment]::NewLine)"
    [System.IO.File]::AppendAllText("c:\custom-script-log.txt", $message)
}

$ErrorActionPreference = "Stop"

try
{
    Log "Starting script."
    Log "VM agent blob url: $vmAgentBlobUrl"
    Log "Creating vsoagent directory..."
    Set-Location "c:\"
    if (-Not (Test-Path "vsonline\vsoagent\bin\appdata"))
    {
        New-Item "vsonline\vsoagent\bin\appdata" -ItemType Directory 
        Set-Location "vsonline\vsoagent"
        Log "Download the VSO agent..."
        Invoke-WebRequest -Uri $vmAgentBlobUrl -OutFile "vsoagent.zip"
        Expand-Archive -Path "vsoagent.zip" -DestinationPath "bin"
        Remove-Item "vsoagent.zip" -Force
    }
    else
    {
        Set-Location "vsonline\vsoagent"
    }

    # Write to .ini file agent reads to pick up configuration.
    if (-Not (Test-Path "bin\config.ini"))
    {
        Log "Writing config.ini..."
        Add-Content "bin\config.ini" "[ENVAGENTSETTINGS]"
        Add-Content "bin\config.ini" "INPUTQUEUENAME=$vmAgentInputQueueName"
        Add-Content "bin\config.ini" "INPUTQUEUEURL=$vmAgentInputQueueUrl"
        Add-Content "bin\config.ini" "INPUTQUEUESASTOKEN=$vmAgentInputQueueSasToken"
    }

    # Create file that will run for the user on startup.
    Log "Write start up file..."
    $startUpFile = "C:\Users\$username\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\vsoagent.cmd"
    if (-Not (Test-Path $startUpFile))
    {
        Add-Content $startUpFile 'C:\VisualStudio\Common7\IDE\VsRegEdit.exe set "C:\VisualStudio" HKCU FeatureFlags\ServiceBroker\LiveShareTransport Value DWORD 1'
        Add-Content $startUpFile 'C:\VisualStudio\Common7\IDE\VsRegEdit.exe set "C:\VisualStudio" HKCU FeatureFlags\Microsoft\VisualStudio\Terminal Value DWORD 1'
        Add-Content $startUpFile "cd c:\vsonline\vsoagent\bin"
        Add-Content $startUpFile "c:\vsonline\vsoagent\bin\vso.exe vmagent"
    }

    # Schedule a restart to run the start up program.
    Log "Scheduling restart..."
    & $env:SystemRoot\system32\shutdown.exe -r -f -t 60
    Log "Finished."
}
catch 
{
    Log "There was an error during script execution:"
    Log $_
    throw
}