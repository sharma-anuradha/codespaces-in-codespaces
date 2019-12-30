#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/helpers.sh

# Apps still target 2.2 runtime, so install 2.2 runtime so we can execute unit tests
# TODO: Remove once apps target 3.1 at runtime
execute "wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh"
execute "chmod +x dotnet-install.sh"
execute "./dotnet-install.sh --version 2.2.8 --runtime aspnetcore --install-dir /usr/share/dotnet"
execute "rm dotnet-install.sh"

execute "dotnet restore /m /v:m /p:BuildPortal=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"
