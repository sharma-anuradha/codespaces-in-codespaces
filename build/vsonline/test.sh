#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

execute "dotnet test --no-build --configuration Release --filter Category!=IntegrationTest /v:m /p:BuildPortal=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"
