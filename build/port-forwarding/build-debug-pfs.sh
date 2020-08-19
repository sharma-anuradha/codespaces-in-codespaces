#!/bin/bash

# To run example
#
#   ./build-pfs.sh --alias pelisy --pfs-tag dev-pelisy-002 --portal-tag dev-pelisy-003 --pfa-tag dev-pelisy-003
#

# Stop script on NZEC
set -e
# Stop script if unbound variable found (use ${var:-} if intentional)
set -u
# By default cmd1 | cmd2 returns exit code of cmd2 regardless of cmd1 success
# This is causing it to fail
set -o pipefail

source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

execute "dotnet publish --no-restore /m /v:m /p:BuildFrontendBackend=false /p:BuildPortForwarding=true /p:BuildPortal=false /p:BuildTokenService=false /p:BuildOtherServices=false /p:BuildOps=false $REPO_ROOT/dirs.proj"
execute "docker build -f $REPO_ROOT/bin/debug/PortForwardingWebApi/Dockerfile.debug $REPO_ROOT/bin/debug/PortForwardingWebApi -t port-forwarding-web-api:debug"
