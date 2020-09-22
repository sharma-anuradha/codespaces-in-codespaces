param(
    [Parameter(Mandatory)]
    [string]$AppSettingsFile,
    [Parameter(Mandatory)]
    [string]$ImageVersion,
    [string]$VsChannelUrl,
    [string]$VsVersion,
    [switch]$InternalImage,
    [switch]$IsPromotion,
    [switch]$UseServerOs
)
$Content = Get-Content $AppSettingsFile

$Pattern = 'Windows'
if ($UseServerOs) {
    $Pattern += 'Server'
}
if ($InternalImage) {
    $Pattern += 'Internal'
}
if (!$IsPromotion) {
    $Pattern += 'Staging'
}

Write-Host "Updating $Pattern in $AppSettingsFile"
for ($Index = 0; $Index -lt $Content.Count;) {
    if ($Content[$Index++] -match """imageName"": ""$Pattern"",") {
        $Content[$Index] = $Content[$Index] -replace '(\d{4}\.){2}\d{3}', $ImageVersion
        if ($VsChannelUrl) {
            $Content[$Index+1] = $Content[$Index+1] -replace ': "[^"]*"', ": ""$VsChannelUrl"""
        }
        if ($VsVersion) {
            $Content[$Index+2] = $Content[$Index+2] -replace ': "[^"]*"', ": ""$VsVersion"""
        }
    }
}
# Strip the final newline that Get-Content adds before writing to file
$Output = $Content | Out-String
$Output.Substring(0, $Output.LastIndexOf('}') + 1) | Set-Content $AppSettingsFile -NoNewline
