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
    [string]$WorkPath
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$SourceSubscription = 'cd02057e-7b97-4159-b924-e8392142ee1e'

$RegionCode = @{eastus='use';westus2='usw2';westeurope='euw';southeastasia='asse'}.$TargetLocation
if ($null -eq $RegionCode) {
    throw "Cannot convert $TargetLocation to a region code"
}

az account set -s $SourceSubscription

$SourceImage = (az image list | ConvertFrom-Json) | Where-Object name -eq $ImageName
if ($null -eq $SourceImage) {
    throw "Cannot find $ImageName image in $SourceSubscription subscription"
}
if ($SourceImage -is [object[]]) {
    throw "Found more than one image named $ImageName in $SourceSubscription subscription"
}
$TagNames = $SourceImage.tags | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
Write-Host "Found $($TagNames -join ' & ') Tags in Source Image"

# Download and extract the latest v10 version of azCopy to our work folder
$AzCopyZipPath = Join-Path $WorkPath "azCopy.zip"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://aka.ms/downloadazcopy-v10-windows" -OutFile $AzCopyZipPath -UseBasicParsing
Expand-Archive -Path $AzCopyZipPath -DestinationPath $WorkPath -Force
$AzCopyExe = Get-ChildItem $WorkPath/*/azcopy.exe

Write-Host "Snapshot $ImageName"
$SnapshotName = "$ImageName-$Environment-$RegionCode"
az snapshot create -n $SnapshotName -g $SourceImage.resourceGroup --source $SourceImage.storageProfile.osDisk.managedDisk.id

$TargetSubscription = "vsclk-core-$Environment"
$TargetGroupName = "vsclk-online-$Environment-images-$RegionCode"
$TempStorageName = "$ImageName$Environment$RegionCode".ToLower()
Write-Host "Create $TempStorageName Storage Account"
$Storage = az storage account create -n $TempStorageName -g $TargetGroupName -l $TargetLocation --subscription $TargetSubscription --kind StorageV2 --sku Premium_LRS --https-only true | ConvertFrom-Json
$Storage

$TempContainerName = "snapshot"
Write-Host "Create $TempContainerName Storage Container"
az storage container create -n $TempContainerName --account-name $TempStorageName --subscription $TargetSubscription

Write-Host "Grant AzCopy Access to Copy $SnapshotName"
$SourceAccess = az snapshot grant-access --duration-in-seconds 36000 -n $SnapshotName -g $SourceImage.resourceGroup | ConvertFrom-Json
$ExpiryDate = (Get-Date).AddSeconds(36000).ToUniversalTime().ToString('s') + "Z"
$TargetAccess = az storage blob generate-sas --container-name $TempContainerName --account-name $TempStorageName --name $ImageName --permissions acdrw --full-uri --expiry $ExpiryDate --subscription $TargetSubscription

Write-Host "Copy $SnapshotName from $SourceSubscription to $TargetSubscription"
& $AzCopyExe copy $SourceAccess.accessSas $TargetAccess

Write-Host "Snapshot $ImageName"
$Snapshot = az snapshot create -n $SnapshotName -g $TargetGroupName -l $TargetLocation --source "$($Storage.primaryEndpoints.blob)$TempContainerName/$ImageName" --source-storage-account-id $Storage.id --subscription $TargetSubscription | ConvertFrom-Json
if (-not $Snapshot) {
    Write-Error "Snapshot $ImageName Failed"
}
$Snapshot

Write-Host "Create $ImageName Image"
$TargetImage = az image create -n $ImageName -g $TargetGroupName -l $TargetLocation --os-type $SourceImage.storageProfile.osDisk.osType --source $Snapshot.id --subscription $TargetSubscription
if (-not $TargetImage) {
    Write-Error "Create $ImageName Image Failed"
}
$TargetImage
$TagNames | ForEach-Object {
    Write-Host "Create $_ Image Tag"
    az image update -n $ImageName -g $TargetGroupName --set "tags.$_=$($SourceImage.tags.$_)" --subscription $TargetSubscription
}

Write-Host "Remove $SnapshotName"
az snapshot revoke-access -n $SnapshotName -g $SourceImage.resourceGroup
az snapshot delete -n $SnapshotName -g $SourceImage.resourceGroup
az snapshot delete -n $SnapshotName -g $TargetGroupName --subscription $TargetSubscription

Write-Host "Remove $TempStorageName Storage Account"
az storage account delete -n $TempStorageName -g $TargetGroupName --subscription $TargetSubscription --yes

$ImageDefinitionName = 'windows'
$ImageGalleryName = "gallery_$RegionCode"
Write-Host "Create Image Version $ImageVersion for $ImageName"
$ImageDefinitionVersion = az sig image-version create --resource-group $TargetGroupName --gallery-name $ImageGalleryName --gallery-image-definition $ImageDefinitionName --gallery-image-version $ImageVersion --managed-image $ImageName --location $TargetLocation --target-regions $TargetLocation --subscription $TargetSubscription
if (-not $ImageDefinitionVersion) {
    Write-Error "Create Image Version $ImageVersion for $ImageName Failed"
}
$ImageDefinitionVersion
$TagNames | ForEach-Object {
    Write-Host "Create $_ Image Version Tag"
    az sig image-version update --resource-group $TargetGroupName --gallery-name $ImageGalleryName --gallery-image-definition $ImageDefinitionName --gallery-image-version $ImageVersion --set "tags.$_=$($SourceImage.tags.$_)" --subscription $TargetSubscription --no-wait
}

# In the image pipeline (https://dev.azure.com/devdiv/OnlineServices/_releaseDefinition?definitionId=83&_a=definition-pipeline) we set variables to determine the replica count for each location.
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
    az sig image-version update --resource-group $TargetGroupName --gallery-name $ImageGalleryName --gallery-image-definition $ImageDefinitionName --gallery-image-version $ImageVersion --target-regions "$TargetLocation=$ReplicaCount" --subscription $TargetSubscription --no-wait
}
