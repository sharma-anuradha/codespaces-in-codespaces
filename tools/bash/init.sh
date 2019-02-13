#!/bin/bash

export ROOT="$(git rev-parse --show-toplevel)"
export ENVIRONMENT_NAME="$( basename "$ROOT")"
echo "$ENVIRONMENT_NAME dev environment"

# User profile
if [ -f "$HOME/.profile" ]; then
    . "$HOME/.profile"
fi

function echo_dim()
{
    echo -e "\e[37m$1\e[0m"
}

function echo_warning()
{
    echo -e "\e[33m$1\e[0m"
}

function which_tool()
{
    local TOOL=$1
    local INSTALL=$2
    local TOOL_PATH=$(which $TOOL | head -n 1)
    if [ -z "$TOOL_PATH" ]; then
        local MESSAGE="$(printf "%-12s" $TOOL) : not found on path"
        if [ -n "$INSTALL" ]; then
            MESSAGE="$MESSAGE, see $INSTALL"
        fi
        echo_warning "$MESSAGE"
        return 1
    else
        echo_dim "$(printf "%-12s" $TOOL) : $TOOL_PATH"
        return 0
    fi
}

# Local initialization
alias root='pushd "$ROOT" > /dev/null'
alias src='pushd "$ROOT/src" > /dev/null'
alias out='pushd "$ROOT/out/bin/Debug" > /dev/null'

# Dev tools
echo "$ENVIRONMENT_NAME tools:"
which_tool "az" "https://dotnet.microsoft.com/download/linux-package-manager/ubuntu14-04/sdk-current" 
which_tool "code"
which_tool "devenv.exe"
which_tool "dotnet" "https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-apt?view=azure-cli-latest"
which_tool "git" "https://git-scm.com/download/linux"
which_tool "helm" "https://github.com/helm/helm/blob/master/docs/install.md"
which_tool "istioctl" "https://istio.io/docs/setup/kubernetes/quick-start/"
which_tool "kubectl" "https://kubernetes.io/docs/tasks/tools/install-kubectl/#install-kubectl-binary-using-curl" && alias k='kubectl'
which_tool "npm"
which_tool "python3" "https://docs.python-guide.org/starting/install3/linux/"
