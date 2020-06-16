#Requires -Version 7.0
param (
    [string]$repoPath = "."
)

$ErrorActionPreference = "Stop"

$scriptFolder = (Get-Item $PSCommandPath).DirectoryName
$modulesFolder = Resolve-Path (Join-Path $scriptFolder .. modules)

. (Join-Path $modulesFolder FindReplace FindReplace.ps1)
. (Join-Path $modulesFolder Git Git.ps1)

function RenameRepoFolder($parentPath, $from, $to) {
    _Git mv "$parentPath/${from}" "$parentPath/${to}"
    _Git commit -m "Move ${from} to ${to}"

    ReplaceInFiles -path $repoPath -queryRegex "(?<=(/|\\))${from}(?![a-zA-Z\.])" -replaceText $to
    ReplaceInFiles -path $repoPath -queryRegex "`"${from}`"" -replaceText "`"${to}`"" -ignoreCase $false
    _Git add .
    _Git commit -m "s/${from}/${to}"
}

$repoPath = Resolve-Path $repoPath

try {
    Push-Location $repoPath

    _Git rm -r src/services/lib
    ReplaceInFiles -path src/services -queryRegex "\r?\nProject.*VsClk\.Hosting.*\r?\nEndProject" -replaceText "" -ignoreCase $false -multiline $true -filterGlob "*.sln"
    ReplaceInFiles -path src/services -queryRegex "\r?\nProject.*`"lib`".*\r?\nEndProject" -replaceText "" -ignoreCase $false -multiline $true -filterGlob "*.sln"
    ReplaceInFiles -path src/services -queryRegex "\r?\n.*lib.*$" -replaceText "" -ignoreCase $false -multiline $true -filterGlob "*.proj"
    _Git add .
    _Git commit -m "Remove unused src/services/lib"

    RenameRepoFolder -parentPath "src" -from "BackEnd" -to "Resources"
    RenameRepoFolder -parentPath "src" -from "FrontEnd" -to "Codespaces"
}
finally {
    Pop-Location
}