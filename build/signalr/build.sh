#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

execute "dotnet publish --no-restore --configuration Release /m /v:m $REPO_ROOT/src/services/containers/VsClk.SignalService/dirs.proj"
execute "dotnet build /m /v:m $REPO_ROOT/src/services/deploy/service-deploy.proj"
