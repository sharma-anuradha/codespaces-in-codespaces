#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

execute "dotnet publish --no-restore --configuration Release /m /v:m /p:BuildFrontendBackend=false /p:BuildPortForwarding=false /p:BuildPortal=true /p:BuildTokenService=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"
