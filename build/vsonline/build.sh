#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/helpers.sh

echo "Build Nuget packages"
execute "dotnet pack --include-source --include-symbols --no-restore --configuration Release /m /v:m /p:BuildPortal=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"

execute "dotnet publish --no-restore --configuration Release /m /v:m /p:BuildPortal=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"
