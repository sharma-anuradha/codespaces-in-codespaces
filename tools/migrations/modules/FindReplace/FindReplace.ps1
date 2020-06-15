$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
$utf8WithBom = New-Object System.Text.UTF8Encoding($true)

$ErrorActionPreference = "Stop"

# Verify module requirements
if ($null -eq (Get-Command rg -ErrorAction SilentlyContinue)) {
    Write-Error -Message "Unable to find ripgrep (rg) in your PATH. Try ``choco install ripgrep`` or see https://github.com/BurntSushi/ripgrep#installation for other options."
    return
}

function FilesWithMatches($path, $queryRegex, [bool]$ignoreCase = $true, [bool]$multiline = $false, $filterGlob = "") {
    $caseFlag = "-i"
    if (!$ignoreCase) {
        $caseFlag = ""
    }
    $multilineFlag = ""
    if ($multiline) {
        $multilineFlag = "-U"
    }
    $globFlag = ""
    if ($filterGlob) {
        $globFlag = "-g","${filterGlob}"
    }
    $result = rg $caseFlag $multilineFlag --hidden -P --files-with-matches "${queryRegex}" $globFlag $path
    return $result
}

function ReplaceInFile($filePath, $queryRegex, $replaceText, [bool]$ignoreCase = $true, [bool]$multiline = $false) {
    # Get encoding so we can preserve BOM/no-BOM
    $encoding = $utf8WithoutBom
    [byte[]]$byte = Get-Content -AsByteStream -ReadCount 4 -TotalCount 4 -Path $filePath
    if ( $byte[0] -eq 0xef -and $byte[1] -eq 0xbb -and $byte[2] -eq 0xbf ) {
        $encoding = $utf8WithBom
    }

    $content = [IO.File]::ReadAllText($filePath, $encoding)

    $regexOptions = [System.Text.RegularExpressions.RegexOptions]::None
    if ($ignoreCase) {
        $regexOptions = $regexOptions -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
    }
    if ($multiline) {
        $regexOptions = $regexOptions -bor [System.Text.RegularExpressions.RegexOptions]::Multiline
    }
    $updatedContent = [System.Text.RegularExpressions.Regex]::Replace($content, $queryRegex, $replaceText, $regexOptions)

    [IO.File]::WriteAllText($filePath, $updatedContent, $encoding)
}

function ReplaceInFiles($path, $queryRegex, $replaceText, [bool]$ignoreCase = $true, [bool]$multiline = $false, $filterGlob = "") {
    FilesWithMatches -path $path -queryRegex $queryRegex -ignoreCase $ignoreCase -multiline $multiline -filterGlob $filterGlob | ForEach-Object {
        $filePath = $_ | Resolve-Path
        ReplaceInFile -filePath $filePath -queryRegex $queryRegex -replaceText $replaceText -ignoreCase $ignoreCase -multiline $multiline
    }
}