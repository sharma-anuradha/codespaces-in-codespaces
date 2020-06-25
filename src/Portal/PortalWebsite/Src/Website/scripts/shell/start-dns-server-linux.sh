#!/bin/sh

# If we're running in WSL, try to run the Windows version of the script..
cd $(dirname $0)
if ! powershell.exe -File start-dns-server-win32.ps1; then
    echo "** No DNS server started, please modify your hosts file by hand. **"
fi
