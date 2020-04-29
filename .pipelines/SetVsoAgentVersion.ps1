param(
    [Parameter(Mandatory)]
    [string]$AppSettingsFile,
    [Parameter(Mandatory)]
    [string]$AgentVersion
)
$Content = Get-Content $AppSettingsFile
$LinuxPattern = 'VSOAgent_linux_(.)+.zip'
$WinPattern = 'VSOAgent_win_(.)+.zip'
$OsxPattern = 'VSOAgent_osx_(.)+.zip'
for ($Index = 0; $Index -lt $Content.Count; $Index++) {
    if ($Content[$Index] -match """imageName"": ""$LinuxPattern""") {
        Write-Host "Updating $LinuxPattern in $AppSettingsFile"
        $Content[$Index] = $Content[$Index] -replace '(\d{7})', $AgentVersion
    } elseif ($Content[$Index] -match """imageName"": ""$WinPattern""") {
        Write-Host "Updating $WinPattern in $AppSettingsFile"
        $Content[$Index] = $Content[$Index] -replace '(\d{7})', $AgentVersion
    } elseif ($Content[$Index] -match """imageName"": ""$OsxPattern""") {
        Write-Host "Updating $OsxPattern in $AppSettingsFile"
        $Content[$Index] = $Content[$Index] -replace '(\d{7})', $AgentVersion
    }
}
# Strip the final newline that Get-Content adds before writing to file
$Output = $Content | Out-String
$Output.Substring(0, $Output.LastIndexOf('}') + 1) | Set-Content $AppSettingsFile -NoNewline
