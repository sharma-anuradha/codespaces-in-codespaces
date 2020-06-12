#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

execute "dotnet test --no-restore --configuration Release $REPO_ROOT/test/services/containers/VsClk.SignalService/dirs.proj"
