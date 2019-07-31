#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

<#
.SYNOPSIS
    Installs node.js
.DESCRIPTION
    Installs node.js. If node.js installation already exists in the given directory
    it will update it only if the requested version differs from the one already installed.
.PARAMETER Version
    Default: latest
    Represents a build version on specific channel. Possible values:
    - latest - most latest build on specific channel
    - coherent - most latest coherent build on specific channel
          coherent applies only to SDK downloads
    - 3-part version in a format A.B.C - represents specific version of build
          examples: 2.0.0-preview2-006120; 1.1.0
.PARAMETER InstallDir
    Default: %LocalAppData%\NodeJS\node
    Path to where to install NodeJs. Note that binaries will be placed directly in a given directory.
.PARAMETER Architecture
    Default: <auto> - this value represents currently running OS architecture
    Architecture of NodeJs binaries to be installed.
    Possible values are: <auto>, x64 and x86
.PARAMETER DryRun
    If set it will not perform installation but instead display what command line to use to consistently install
    currently requested version of node.js. In example if you specify version 'latest' it will display a link
    with specific version so that this command can be used deterministicly in a build script.
    It also displays binaries location if you prefer to install or download it yourself.
.PARAMETER NoPath
    By default this script will set environment variable PATH for the current process to the binaries folder inside installation folder.
    If set it will display binaries location but not set any environment variable.
.PARAMETER Verbose
    Displays diagnostics information.
.PARAMETER NodeDistEndpoint
    This parameter typically is not changed by the user.
    It allows to change URL for the node dist feed used by this installer.
.PARAMETER X64Hash
    Hash of the x64 node distribution zip file.
.PARAMETER X86Hash
    Hash of the x86 node distribution zip file.
.PARAMETER ProxyAddress
    If set, the installer will use the proxy when making web requests
.PARAMETER ProxyUseDefaultCredentials
    Default: false
    Use default credentials, when using proxy address.
#>
[cmdletbinding()]
param(
   [string]$Version="Latest",
   [string]$InstallDir="<auto>",
   [string]$Architecture="<auto>",
   [switch]$DryRun,
   [switch]$NoPath,
   [string]$NodeDistEndpoint="https://nodejs.org/dist",
   [string]$X64Hash,
   [string]$X86Hash,
   [string]$ProxyAddress,
   [switch]$ProxyUseDefaultCredentials
)

Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

$BinFolderRelativePath=""

function Say($str) {
    Write-Host "node-install: $str"
}

function Say-Verbose($str) {
    Write-Verbose "node-install: $str"
}

function Say-Invocation($Invocation) {
    $command = $Invocation.MyCommand;
    $args = (($Invocation.BoundParameters.Keys | foreach { "-$_ `"$($Invocation.BoundParameters[$_])`"" }) -join " ")
    Say-Verbose "$command $args"
}

function Invoke-With-Retry([ScriptBlock]$ScriptBlock, [int]$MaxAttempts = 3, [int]$SecondsBetweenAttempts = 1) {
    $Attempts = 0

    while ($true) {
        try {
            return $ScriptBlock.Invoke()
        }
        catch {
            $Attempts++
            if ($Attempts -lt $MaxAttempts) {
                Start-Sleep $SecondsBetweenAttempts
            }
            else {
                throw
            }
        }
    }
}

function Get-Machine-Architecture() {
    Say-Invocation $MyInvocation

    # possible values: AMD64, IA64, x86
    return $ENV:PROCESSOR_ARCHITECTURE
}

# TODO: Architecture and CLIArchitecture should be unified
function Get-CLIArchitecture-From-Architecture([string]$Architecture) {
    Say-Invocation $MyInvocation

    switch ($Architecture.ToLower()) {
        { $_ -eq "<auto>" } { return Get-CLIArchitecture-From-Architecture $(Get-Machine-Architecture) }
        { ($_ -eq "amd64") -or ($_ -eq "x64") } { return "x64" }
        { $_ -eq "x86" } { return "x86" }
        default { throw "Architecture not supported." }
    }
}

function Load-Assembly([string] $Assembly) {
    try {
        Add-Type -Assembly $Assembly | Out-Null
    }
    catch {
        # On Nano Server, Powershell Core Edition is used.  Add-Type is unable to resolve base class assemblies because they are not GAC'd.
        # Loading the base class assemblies is not unnecessary as the types will automatically get resolved.
    }
}

function GetHTTPResponse([Uri] $Uri)
{
    Invoke-With-Retry(
    {

        $HttpClient = $null

        try {
            # HttpClient is used vs Invoke-WebRequest in order to support Nano Server which doesn't support the Invoke-WebRequest cmdlet.
            Load-Assembly -Assembly System.Net.Http

            if(-not $ProxyAddress) {
                try {
                    # Despite no proxy being explicitly specified, we may still be behind a default proxy
                    $DefaultProxy = [System.Net.WebRequest]::DefaultWebProxy;
                    if($DefaultProxy -and (-not $DefaultProxy.IsBypassed($Uri))) {
                        $ProxyAddress = $DefaultProxy.GetProxy($Uri).OriginalString
                        $ProxyUseDefaultCredentials = $true
                    }
                } catch {
                    # Eat the exception and move forward as the above code is an attempt
                    #    at resolving the DefaultProxy that may not have been a problem.
                    $ProxyAddress = $null
                    Say-Verbose("Exception ignored: $_.Exception.Message - moving forward...")
                }
            }

            if($ProxyAddress) {
                $HttpClientHandler = New-Object System.Net.Http.HttpClientHandler
                $HttpClientHandler.Proxy =  New-Object System.Net.WebProxy -Property @{Address=$ProxyAddress;UseDefaultCredentials=$ProxyUseDefaultCredentials}
                $HttpClient = New-Object System.Net.Http.HttpClient -ArgumentList $HttpClientHandler
            }
            else {
                $HttpClient = New-Object System.Net.Http.HttpClient
            }
            # Default timeout for HttpClient is 100s.  For a 50 MB download this assumes 500 KB/s average, any less will time out
            # 10 minutes allows it to work over much slower connections.
            $HttpClient.Timeout = New-TimeSpan -Minutes 10
            $Response = $HttpClient.GetAsync($Uri).Result
            if (($Response -eq $null) -or (-not ($Response.IsSuccessStatusCode))) {
                $ErrorMsg = "Failed to download $Uri."
                if ($Response -ne $null) {
                    $ErrorMsg += "  $Response"
                }

                throw $ErrorMsg
            }

             return $Response
        }
        finally {
             if ($HttpClient -ne $null) {
                $HttpClient.Dispose()
            }
        }
    })
}

function Get-Download-Link([string]$FeedURL, [string]$Version, [string]$CLIArchitecture) {
    Say-Invocation $MyInvocation

    $PayloadURL = "$FeedURL/$Version/node-$Version-win-$CLIArchitecture.zip"

    Say-Verbose "Constructed primary payload URL: $PayloadURL"

    return $PayloadURL
}

function Get-User-Share-Path() {
    Say-Invocation $MyInvocation

    $InstallRoot = $env:NODEJS_INSTALL_DIR
    if (!$InstallRoot) {
        $InstallRoot = "$env:LocalAppData\NodeJS\node"
    }
    return $InstallRoot
}

function Resolve-Installation-Path([string]$InstallDir) {
    Say-Invocation $MyInvocation

    if ($InstallDir -eq "<auto>") {
        return Get-User-Share-Path
    }
    return $InstallDir
}

function Get-Absolute-Path([string]$RelativeOrAbsolutePath) {
    # Too much spam
    # Say-Invocation $MyInvocation

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RelativeOrAbsolutePath)
}

# Example zip content and extraction algorithm:
# Rule: files if extracted are always being extracted to the same relative path locally
# .\
#       a.exe   # file does not exist locally, extract
#       b.dll   # file exists locally, override only if $OverrideFiles set
#       aaa\    # same rules as for files
#           ...
#       abc\1.0.0\  # directory contains version and exists locally
#           ...     # do not extract content under versioned part
#       abc\asd\    # same rules as for files
#            ...
#       def\ghi\1.0.1\  # directory contains version and does not exist locally
#           ...         # extract content
function Extract-Node-Package([string]$ZipPath, [string]$OutPath) {
    
    Say-Invocation $MyInvocation

    Load-Assembly -Assembly System.IO.Compression.FileSystem
    Set-Variable -Name Zip
    try {
        $Zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)

        foreach ($entry in $Zip.Entries) {
            Say-Verbose "Extracting: $entry"
            $DestinationPath = Get-Absolute-Path $(Join-Path -Path $OutPath -ChildPath $entry.FullName)
            $DestinationDir = Split-Path -Parent $DestinationPath
            $OverrideFiles = $true
            if ((-Not $DestinationPath.EndsWith("\")) -And $OverrideFiles) {
                New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
                [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $DestinationPath, $true)
            }
        }
    }
    finally {
        if ($Zip -ne $null) {
            $Zip.Dispose()
        }
    }
}

function DownloadFile([Uri]$Uri, [string]$OutPath) {
    $Stream = $null

    try {
        $Response = GetHTTPResponse -Uri $Uri
        $Stream = $Response.Content.ReadAsStreamAsync().Result
        $File = [System.IO.File]::Create($OutPath)
        $Stream.CopyTo($File)
        $File.Close()
    }
    finally {
        if ($Stream -ne $null) {
            $Stream.Dispose()
        }
    }
}

function Prepend-Sdk-InstallRoot-To-Path([string]$InstallRoot, [string]$BinFolderRelativePath) {
    $BinPath = Get-Absolute-Path $(Join-Path -Path $InstallRoot -ChildPath $BinFolderRelativePath)
    if (-Not $NoPath) {
        Say "Adding to current process PATH: `"$BinPath`". Note: This change will not be visible if PowerShell was run as a child process."
        $env:path = "$BinPath;" + $env:path
    }
    else {
        Say "Binaries of node can be found in $BinPath"
    }
}

function Compare-SHA256-Hash([string]$DownloadFilePath, [string]$ExpectedHash) {
    Say-Invocation $MyInvocation
    
    $hashFromFile = Get-FileHash -Path $DownloadFilePath -Algorithm SHA256
    
    $hashString = $hashFromFile.Hash.ToLower()
    if ($hashString -ne $ExpectedHash) {
        Say  "Hash of downloaded file ($hashString) doesn't match $ExpectedHash."
        throw "Hash of downloaded file ($hashString) doesn't match $ExpectedHash."
    }
}

function Get-Expected-Hash-By-Architecture([string]$CLIArchitecture) {
    switch ($CLIArchitecture) {
        { $_ -eq "x64" } { return $X64Hash }
        { $_ -eq "x86" } { return $X86Hash }
        default { throw "Architecture not supported." }
    }
}

$CLIArchitecture = Get-CLIArchitecture-From-Architecture $Architecture
$DownloadLink = Get-Download-Link -FeedURL $NodeDistEndpoint -Version $Version -CLIArchitecture $CLIArchitecture

if ($DryRun) {
    Say "Payload URLs:"
    Say "Primary - $DownloadLink"
    Say "Repeatable invocation: .\$($MyInvocation.Line)"
    exit 0
}

$BinFolderRelativePath = "node-$Version-win-$CLIArchitecture"
$InstallRoot = Resolve-Installation-Path $InstallDir
$ExtractedAssetsPath = Get-Absolute-Path $(Join-Path -Path $InstallRoot -ChildPath $BinFolderRelativePath)

Say-Verbose "InstallRoot: $InstallRoot"

New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null

try {
    $installDrive = $((Get-Item $InstallRoot).PSDrive.Name);
    $free = Get-CimInstance -Class win32_logicaldisk | where Deviceid -eq "${installDrive}:"
    if ($free.Freespace / 1MB -le 100 ) {
        Say "There is not enough disk space on drive ${installDrive}:"
        exit 0
    }
} catch {
    Say "There might not be enough space on the disk. The check failed."
}


$ZipPath = [System.IO.Path]::combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
Say-Verbose "Zip path: $ZipPath"
Say "Downloading link: $DownloadLink"
try {
    DownloadFile -Uri $DownloadLink -OutPath $ZipPath
}
catch {
    Say "Cannot download: $DownloadLink"
}

Say "Extracting zip from $DownloadLink (file: $ZipPath)"
$ExpectedHash = Get-Expected-Hash-By-Architecture -CLIArchitecture $CLIArchitecture
Compare-SHA256-Hash -DownloadFilePath $ZipPath -ExpectedHash $ExpectedHash

Extract-Node-Package -ZipPath $ZipPath -OutPath $InstallRoot

$ExtractedAssetsPath = Get-Absolute-Path $(Join-Path -Path $InstallRoot -ChildPath $BinFolderRelativePath)
Get-ChildItem -Path $ExtractedAssetsPath | Move-Item -Destination $InstallRoot
Remove-Item $ExtractedAssetsPath
$BinFolderRelativePath = ""

Remove-Item $ZipPath

Prepend-Sdk-InstallRoot-To-Path -InstallRoot $InstallRoot -BinFolderRelativePath $BinFolderRelativePath

Say "Installation finished"
exit 0
