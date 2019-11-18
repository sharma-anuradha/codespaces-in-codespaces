param(
    [Parameter(Mandatory)]
    [string]$AppSettingsFile,
    [Parameter(Mandatory)]
    [string]$ImageVersion
)
$Content = Get-Content $AppSettingsFile
for ($Index = 0; $Index -lt $Content.Count;) {
    if ($Content[$Index++] -match '"imageName": "NexusWindowsImage",') {
        $Content[$Index] = $Content[$Index] -replace '(\d{4}\.){2}\d{3}', $ImageVersion
        break
    }
}
# Strip the final newline that Get-Content adds before writing to file
$Output = $Content | Out-String
$Output.Substring(0, $Output.LastIndexOf('}') + 1) | Set-Content $AppSettingsFile -NoNewline
