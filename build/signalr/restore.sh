#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

execute "dotnet restore /m /v:m $REPO_ROOT/src/services/containers/VsClk.SignalService/dirs.proj"
execute "dotnet restore /m /v:m $REPO_ROOT/test/services/containers/VsClk.SignalService/dirs.proj"
