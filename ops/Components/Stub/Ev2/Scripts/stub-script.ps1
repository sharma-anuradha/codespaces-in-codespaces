# This is a dummy script that dumps environment variables to the host console.
"Environment Variables" | Write-Host -ForegroundColor Green
Get-ChildItem env:* | Sort-Object Name | Out-String | Write-Host -ForegroundColor Green
