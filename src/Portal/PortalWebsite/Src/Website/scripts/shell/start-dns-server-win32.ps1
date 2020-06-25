$OLD_CONTAINER_NAME="vscs-local-dns-server"
$CONTAINER_NAME="portal-local-dns-server"

Write-Output ""
Write-Output "* Querying Docker.."

docker info | out-null
if (!$?) {
    Write-Output "! Docker is not running, please start docker deamon first."
    exit 1
}

# docker is running

Write-Output ""
Write-Output "* Starting the DNS server.."

docker stop $OLD_CONTAINER_NAME 2>&1>$null
docker rm $OLD_CONTAINER_NAME 2>&1>$null

Write-Output "* Starting the DNS server docker container.."

# WSL has a different IP
$isWsl = $PSCommandPath.StartsWith('\\wsl$\')
if ($isWsl) {
    wsl -e ./wsl2-docker-compose.sh
} else {
    docker-compose --env-file ../dev-local.env up -d --build
}

if (!$?) {
    Write-Output "! Could not start the DNS server docker container, terminating.."
    exit 1
}

docker ps -q -f name=$CONTAINER_NAME
if (!$?) {
    Write-Output "! Could not start the DNS server docker container, terminating.."
    exit 1
}

$isAdministrator = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdministrator) {
    $interfaces = (Get-DnsClientServerAddress | Where-Object InterfaceAlias -CNotLike "*Loopback*")
    foreach ($iface in $interfaces) {
        if (($iface.AddressFamily -eq 2) -and (!$iface.ServerAddresses.Contains("127.0.0.1"))) {
            $family = "ipv4"
            $addresses = @("127.0.0.1") + $iface.ServerAddresses
        }
        if (($iface.AddressFamily -eq 23) -and (!$iface.ServerAddresses.Contains("::1"))) {
            $family = "ipv6"
            $addresses = @("::1") + $iface.ServerAddresses
        }
        if ($addresses.Length -gt 1) {
            Write-Output "Setting $family DNS server addresses for '$($iface.InterfaceAlias)' to: $addresses"
            Set-DnsClientServerAddress -InterfaceIndex $iface.InterfaceIndex -ServerAddresses $addresses
        }
    }
    Write-Output ""
} else {
    Write-Output ""
    Write-Output "! Not running as Administrator; you must configure your network settings manually:"
}
Write-Output "Local DNS server started running at 127.0.0.1:53"
