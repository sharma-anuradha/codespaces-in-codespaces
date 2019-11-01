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

$TargetSubName = $TargetSubscription.Substring("vsclk-core-".Length)

# Download and extract the latest v10 version of azCopy to our work folder
$AzCopyZipPath = Join-Path $WorkPath "azCopy.zip"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://aka.ms/downloadazcopy-v10-windows" -OutFile $AzCopyZipPath -UseBasicParsing
Expand-Archive -Path $AzCopyZipPath -DestinationPath $WorkPath -Force
$AzCopyExe = Get-ChildItem $WorkPath/*/azcopy.exe

Write-Host "Snapshot $ImageName"
$SnapshotName = "$($ImageName)_snapshot_$TargetSubName"
az snapshot create -n $SnapshotName -g $SourceImage.resourceGroup --source $SourceImage.storageProfile.osDisk.managedDisk.id

$TempGroupName = "image-copy-$ImageName-$TargetSubName"
Write-Host "Create $TempGroupName Resource Group"
az group create -n $TempGroupName -l $TargetLocation --subscription $TargetSubscription

$TempStorageName = "$TargetLocation$TargetSubName$ImageName".ToLower()
Write-Host "Create $TempStorageName Storage Account"
$Storage = az storage account create -n $TempStorageName -g $TempGroupName -l $TargetLocation --subscription $TargetSubscription --kind StorageV2 --sku Premium_LRS | ConvertFrom-Json

$TempContainerName = "snapshots$TargetSubName"
Write-Host "Create $TempContainerName Storage Container"
az storage container create -n $TempContainerName --account-name $TempStorageName --subscription $TargetSubscription

Write-Host "Obtaining the appropriate SAS Url's needed for copying."
$SourceAccess = az snapshot grant-access --duration-in-seconds 36000 -n $SnapshotName -g $SourceImage.resourceGroup | ConvertFrom-Json
$ExpiryDate = (Get-Date).AddSeconds(36000).ToUniversalTime().ToString('s') + "Z"
$TargetAccess = az storage blob generate-sas --container-name $TempContainerName --account-name $TempStorageName --name $ImageName --permissions acdrw --full-uri --expiry $ExpiryDate --subscription $TargetSubscription

Write-Host "Copy $SnapshotName from $SourceSubscription to $TargetSubscription"
$Start = Get-Date
& $AzCopyExe copy $SourceAccess.accessSas $TargetAccess
$Duration = (Get-Date) - $Start
Write-Host "Copy completed in $($Duration.TotalMinutes) minutes"

Write-Host "Snapshot $ImageName"
$Snapshot = az snapshot create -n $SnapshotName -g $TempGroupName -l $TargetLocation --source "$($Storage.primaryEndpoints.blob)$TempContainerName/$ImageName" --subscription $TargetSubscription | ConvertFrom-Json

Write-Host "Create $ImageName Image"
az image create -n $ImageName -g $TargetGroupName -l $TargetLocation --os-type $SourceImage.storageProfile.osDisk.osType --source $Snapshot.id --subscription $TargetSubscription

Write-Host "Remove $SnapshotName"
az snapshot revoke-access -n $SnapshotName -g $SourceImage.resourceGroup
az snapshot delete -n $SnapshotName -g $SourceImage.resourceGroup

Write-Host "Remove $TempGroupName Resource Group"
az group delete -n $TempGroupName --subscription $TargetSubscription --no-wait --yes
