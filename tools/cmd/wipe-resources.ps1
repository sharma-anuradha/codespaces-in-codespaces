param([string] $subscription, [string] $stamp)

$BatchSize = 50
$script:backgroundJobs = @()
function Wait-Jobs() {
    if ($script:backgroundJobs.Count -gt 0) {
        Write-Host "Waiting for all background deletion jobs to complete..."
        $jobs = Wait-Job -Job $script:backgroundJobs
        Receive-Job $jobs
        Write-Host "Done"
        $script:backgroundJobs = @()
    }
}

function Start-BackgroundJob($Block, $Arguments, $Name) {
    $script:backgroundJobs += Start-Job -ScriptBlock $Block -ArgumentList $Arguments -Name $Name

    # Don't spin up too many PS background jobs at once or you will run out of memory
    if ($script:backgroundJobs.Count -ge $BatchSize) {
        Wait-Jobs
    }
}

Write-Host "Deleting $stamp data plane resources from subscription $subscription"
$resourcegroups = az group list --subscription $subscription | ConvertFrom-Json
$toDelete = $resourcegroups | Where-Object { $_.name -like "$stamp-*" }
$total = $toDelete.Count
$count = 1
$toDelete | ForEach-Object {
    Write-Host "[$count/$total] Deleting $($_.name)"
    $count++
    $block = {
        az group delete --name $args[0] --subscription $args[1] -y --no-wait
    }
    Start-BackgroundJob -Block $block -Arguments @($_.name, $subscription) -Name "DeleteRG_$($_.name)"
}
