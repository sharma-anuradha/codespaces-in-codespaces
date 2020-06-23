#!/bin/bash
source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh


pushd "$REPO_ROOT/src/Portal/PortalWebsite/Src/Website/"
execute "yarn build"
popd

execute "dotnet publish --no-restore --configuration Debug /m /v:m /p:BuildFrontendBackend=false /p:BuildPortForwarding=false /p:BuildPortal=true /p:BuildTokenService=false /p:BuildOtherServices=false $REPO_ROOT/dirs.proj"

execute "cp -R $REPO_ROOT/src/Portal/PortalWebsite/Src/Website/build bin/debug/VsClk.Portal.WebSite/publish/ClientApp"

execute "docker build -t vsclk.portal.website:debug -f $REPO_ROOT/src/Portal/PortalWebsite/Src/Website/build bin/debug/VsClk.Portal.WebSite/publish/Dockerfile.debug $REPO_ROOT/src/Portal/PortalWebsite/Src/Website/build bin/debug/VsClk.Portal.WebSite/publish"
