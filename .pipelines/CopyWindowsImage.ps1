param(
    [Parameter(Mandatory)]
    [string]$ImageName,
    [Parameter(Mandatory)]
    [string]$TargetSubscription,
    [Parameter(Mandatory)]
    [string]$TargetGroupName,
    [Parameter(Mandatory)]
    [string]$WorkPath
)
$ProgressPreference = 'SilentlyContinue'
$SourceSubscription = 'cd02057e-7b97-4159-b924-e8392142ee1e'
$TargetLocation = 'westus2'

az account set -s $SourceSubscription

$SourceImage = (az image list | ConvertFrom-Json) | Where-Object name -eq $ImageName
if ($null -eq $SourceImage) {
    throw "Cannot find $ImageName image in $SourceSubscription subscription"
}
if ($SourceImage -is [object[]]) {
    throw "Found more than one image named $ImageName in $SourceSubscription subscription"
}

# Download and extract the latest v10 version of azCopy to our work folder
$AzCopyZipPath = Join-Path $WorkPath "azCopy.zip"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://aka.ms/downloadazcopy-v10-windows" -OutFile $AzCopyZipPath -UseBasicParsing
Expand-Archive -Path $AzCopyZipPath -DestinationPath $WorkPath -Force
$AzCopyExe = Get-ChildItem $WorkPath/*/azcopy.exe

Write-Host "Snapshot $ImageName"
$SnapshotName = "$($ImageName)_os_disk_snapshot"
az snapshot create -n $SnapshotName -g $SourceImage.resourceGroup --source $SourceImage.storageProfile.osDisk.managedDisk.id

Write-Host "Download $SnapshotName"
$BlobPath = Join-Path $WorkPath "$SnapshotName.vhd"
$Access = az snapshot grant-access --duration-in-seconds 36000 -n $SnapshotName -g $SourceImage.resourceGroup | ConvertFrom-Json
$Start = Get-Date
& $AzCopyExe cp $Access.accessSas $BlobPath --check-md5 LogOnly
$Duration = (Get-Date) - $Start
Write-Host "Download completed in $($Duration.TotalMinutes) minutes"
az snapshot revoke-access -n $SnapshotName -g $SourceImage.resourceGroup
az snapshot delete -n $SnapshotName -g $SourceImage.resourceGroup

$TempGroupName = "image-copy-$ImageName"
Write-Host "Create $TempGroupName Resource Group"
az group create -n $TempGroupName -l $TargetLocation --subscription $TargetSubscription

$TempStorageName = "$TargetLocation$ImageName".ToLower()
Write-Host "Create $TempStorageName Storage Account"
$Storage = az storage account create -n $TempStorageName -g $TempGroupName -l $TargetLocation --subscription $TargetSubscription --kind StorageV2 --sku Premium_LRS | ConvertFrom-Json

$TempContainerName = 'snapshots'
Write-Host "Create $TempContainerName Storage Container"
az storage container create -n $TempContainerName --account-name $TempStorageName --subscription $TargetSubscription

Write-Host "Upload $BlobPath"
$Start = Get-Date
az storage blob upload -c $TempContainerName -f $BlobPath -n $ImageName --account-name $TempStorageName --subscription $TargetSubscription --no-progress
$Duration = (Get-Date) - $Start
Write-Host "Upload completed in $($Duration.TotalMinutes) minutes"
Remove-Item $BlobPath -Force -ErrorAction Continue

Write-Host "Snapshot $ImageName"
$Snapshot = az snapshot create -n $SnapshotName -g $TempGroupName -l $TargetLocation --source "$($Storage.primaryEndpoints.blob)$TempContainerName/$ImageName" --subscription $TargetSubscription | ConvertFrom-Json

Write-Host "Create $ImageName Image"
az image create -n $ImageName -g $TargetGroupName -l $TargetLocation --os-type $SourceImage.storageProfile.osDisk.osType --source $Snapshot.id --subscription $TargetSubscription

Write-Host "Remove $TempGroupName Resource Group"
az group delete -n $TempGroupName --subscription $TargetSubscription --no-wait --yes
