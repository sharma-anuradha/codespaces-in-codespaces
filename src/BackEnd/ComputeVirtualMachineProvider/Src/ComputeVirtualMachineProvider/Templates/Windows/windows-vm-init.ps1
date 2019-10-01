# Custom script extension script that is executed on Windows VMs.
# Dev location: vsodevciusw2vmusw2 under the windows-vm-init container.

param 
(
    [string] $vmAgentBlobUrl, # Blob URL contains SAS token, no need for access key
    [string] $vmAgentInputQueueName,
    [string] $vmAgentInputQueueUrl,
    [string] $vmAgentInputQueueSasToken
)

function Log ([string] $message)
{
    [System.IO.File]::AppendAllText("c:\custom-script-log.txt", $message + ([Environment]::NewLine))
}

$ErrorActionPreference = "Stop"

try
{
    Log "Starting script."
    Log "VM agent blob url: $vmAgentBlobUrl"
    Log "Creating vsoagent directory..."
    Set-Location "c:\"
    New-Item "vsonline\vsoagent\bin\appdata" -ItemType Directory 
    Set-Location "vsonline\vsoagent"

    Log "Download the VSO agent..."
    Invoke-WebRequest -Uri $vmAgentBlobUrl -OutFile "vsoagent.zip"
    Expand-Archive -Path "vsoagent.zip" -DestinationPath "bin"
    Remove-Item "vsoagent.zip" -Force

    # Write to .ini file agent reads to pick up configuration.
    Add-Content "bin\config.ini" "[ENVAGENTSETTINGS]"
    Add-Content "bin\config.ini" "INPUTQUEUENAME=$vmAgentInputQueueName"
    Add-Content "bin\config.ini" "INPUTQUEUEURL=$vmAgentInputQueueUrl"
    Add-Content "bin\config.ini" "INPUTQUEUESASTOKEN=$vmAgentInputQueueSasToken"

    Log "Starting the VM agent service..."
    # TODO: this may need to change if VS cannot be launched correctly from the Windows service.
    & "c:\vsonline\vsoagent\bin\VmAgent\EnvironmentAgent\Scripts\InstallWindowsService.ps1" -serviceName "vsonline-agent" -publishLocation "c:\vsonline\vsoagent\bin" 
    Start-Sleep -Seconds 5
    & sc.exe start "vsonline-agent"

    Log "Finished."
}
catch 
{
    Log "There was an error during script execution:"
    Log $_
    throw
}
