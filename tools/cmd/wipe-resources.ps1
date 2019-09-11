param([string] $subscription, [string] $stamp)

Write-Host "Deleting $stamp data plane resources from subscription $subscription"
$resourcegroups = az group list --subscription $subscription | ConvertFrom-Json
$toDelete = $resourcegroups | Where-Object { $_.name -like "$stamp-*" }
$total = $toDelete.Count
$count = 1
$toDelete | ForEach-Object {
    Write-Host "[$count/$total] Deleting $($_.name)"
    az group delete --name $_.name --subscription $subscription -y --no-wait
    $count++
}