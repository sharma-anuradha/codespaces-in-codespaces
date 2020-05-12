#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

pushd "$REPO_ROOT/src/Portal/PortalWebsite/Src/Website/"
execute "yarn test:ci"
popd

execute "dotnet test --no-build --configuration Release --filter Category!=IntegrationTest /v:m /p:BuildFrontendBackend=false /p:BuildPortForwarding=false /p:BuildPortal=true /p:BuildTokenService=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"
