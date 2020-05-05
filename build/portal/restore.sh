#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

execute "export DEBIAN_FRONTEND=noninteractive"
execute "curl -sL https://deb.nodesource.com/setup_12.x | sudo -E bash -"
execute "sudo apt-get install -y nodejs"
execute "npm i -gf yarn"

pushd "$REPO_ROOT/src/services/containers/VsClk.Portal.WebSite/ClientApp/"
execute "yarn --network-timeout 1000000 --frozen-lockfile"
execute "yarn build"
execute "yarn test:ci"
popd

execute "dotnet restore /m /v:m /p:BuildFrontendBackend=false /p:BuildPortForwarding=false /p:BuildPortal=true /p:BuildTokenService=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"
