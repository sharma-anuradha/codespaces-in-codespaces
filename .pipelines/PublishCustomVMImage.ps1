# Script to publish custom Linux VM images to shared image galleries in all stamp locations.

param(
    [Parameter(Mandatory)]
    [string]$ImageName,
    [Parameter(Mandatory)]
    [string]$ImageVersion,
    [Parameter(Mandatory)]
    [string]$TargetLocation,
    [Parameter(Mandatory)]
    [string]$Environment,
    [Parameter(Mandatory)]
    [string]$WorkPath,
    [Parameter(Mandatory)]
    [string]$ImageOsType
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$RegionCode = @{eastus='use';westus2='usw2';westeurope='euw';southeastasia='asse';eastus2euap='usec'}.$TargetLocation
if ($null -eq $RegionCode) {
    throw "Cannot convert $TargetLocation to a region code"
}

# Download and extract the latest v10 version of azCopy to our work folder
$AzCopyZipPath = Join-Path $WorkPath "azCopy.zip"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://aka.ms/downloadazcopy-v10-windows" -OutFile $AzCopyZipPath -UseBasicParsing
Expand-Archive -Path $AzCopyZipPath -DestinationPath $WorkPath -Force
$AzCopyExe = Get-ChildItem $WorkPath/*/azcopy.exe

$TargetGroupName = "vsclk-online-$Environment-images-$RegionCode"
$TempStorageName = "$ImageName$Environment$RegionCode".ToLower()
Write-Host "Create $TempStorageName Storage Account"
$Storage = az storage account create -n $TempStorageName -g $TargetGroupName -l $TargetLocation --kind StorageV2 --sku Premium_LRS --https-only true | ConvertFrom-Json
$Storage

$TempContainerName = "snapshot"
Write-Host "Create $TempContainerName Storage Container"
az storage container create -n $TempContainerName --account-name $TempStorageName

Write-Host "Grant AzCopy Access to Copy snapshot"
$ExpiryDate = (Get-Date).AddSeconds(36000).ToUniversalTime().ToString('s') + "Z"
$TargetAccess = az storage blob generate-sas --container-name $TempContainerName --account-name $TempStorageName --name $ImageName --permissions acdrw --full-uri --expiry $ExpiryDate

Write-Host "Copy snapshot from source to target"
& $AzCopyExe copy $env:SNAPSHOT_SAS $TargetAccess

$SnapshotName = $ImageName
Write-Host "Create snapshot $SnapshotName at target"
$Snapshot = az snapshot create -n $SnapshotName -g $TargetGroupName -l $TargetLocation --source "$($Storage.primaryEndpoints.blob)$TempContainerName/$ImageName" --source-storage-account-id $Storage.id | ConvertFrom-Json
if (-not $Snapshot) {
    Write-Error "Snapshot $SnapshotName Failed"
}
$Snapshot

Write-Host "Create $ImageName Image"
$TargetImage = az image create -n $ImageName -g $TargetGroupName -l $TargetLocation --os-type $ImageOsType --source $Snapshot.id
if (-not $TargetImage) {
    Write-Error "Create $ImageName Image Failed"
}
$TargetImage

Write-Host "Remove $SnapshotName"
az snapshot delete -n $SnapshotName -g $TargetGroupName

Write-Host "Remove $TempStorageName Storage Account"
az storage account delete -n $TempStorageName -g $TargetGroupName --yes

$ImageDefinitionName = 'ubuntu'
$ImageGalleryName = "gallery_$RegionCode"
Write-Host "Create Image Version $ImageVersion for $ImageName"
$ImageDefinitionVersion = az sig image-version create --resource-group $TargetGroupName --gallery-name $ImageGalleryName --gallery-image-definition $ImageDefinitionName --gallery-image-version $ImageVersion --managed-image $ImageName --location $TargetLocation --target-regions "$TargetLocation=Premium_LRS"
if (-not $ImageDefinitionVersion) {
    Write-Error "Create Image Version $ImageVersion for $ImageName Failed"
}
$ImageDefinitionVersion

# In the linux image pipeline we set variables to determine the replica count for each location.
# These are named ReplicaCount.{location} and ADO makes these available to scripts in the pipe as env vars named REPLICACOUNT_{location}.
# So we'll try to find that var for $TargetLocation to set the replica count for the image. If we don't find it, we'll output a warning.
$ReplicaCount = (Get-ChildItem env: | Where-Object Name -eq "REPLICACOUNT_$TargetLocation").Value
if ($null -eq $ReplicaCount) {
    Write-Warning "Skip Create Replicas of Image Version $ImageVersion--Cannot Find Replica Count for $TargetLocation Location"
}
elseif ($ReplicaCount -lt 2) {
    Write-Host "Skip Create Replicas of Image Version $ImageVersion--Requested Count is $ReplicaCount"
}
else {
    Write-Host "Create $ReplicaCount Replicas of Image Version $ImageVersion"
    az sig image-version update --resource-group $TargetGroupName --gallery-name $ImageGalleryName --gallery-image-definition $ImageDefinitionName --gallery-image-version $ImageVersion --target-regions "$TargetLocation=$ReplicaCount" --no-wait
}
