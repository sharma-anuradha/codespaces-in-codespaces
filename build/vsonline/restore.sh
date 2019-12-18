#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/helpers.sh

execute "dotnet restore /m /v:m /p:BuildPortal=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"
