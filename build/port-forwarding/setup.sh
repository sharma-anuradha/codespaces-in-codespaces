#!/bin/bash

# To run example 
# 
#   ./setup.sh --alias pelisy --pfs-tag dev-pelisy-002 --portal-tag dev-pelisy-003 --pfa-tag dev-pelisy-003
# 

# Stop script on NZEC
set -e
# Stop script if unbound variable found (use ${var:-} if intentional)
set -u
# By default cmd1 | cmd2 returns exit code of cmd2 regardless of cmd1 success
# This is causing it to fail
set -o pipefail

source $( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../common/helpers.sh

# Set the subscription for resolving remote state properly
az account set --sub vsclk-core-dev

user_alias=""
pfs_tag=""
pfa_tag=""
portal_tag=""
verbose=false

while [ $# -ne 0 ]
do
    name="$1"
    case "$name" in
        -a|--alias|-[Aa]lias)
            shift
            user_alias="$1"
            ;;
        --pfs-tag)
            shift
            pfs_tag="$1"
            ;;
        --pfa-tag)
            shift
            pfa_tag="$1"
            ;;
        --portal-tag)
            shift
            portal_tag="$1"
            ;;
        --verbose|-[Vv]erbose)
            verbose=true
            non_dynamic_parameters+=" $name"
            ;;
        *)
            say_err "Unknown argument \`$name\`"
            exit 1
            ;;
    esac

    shift
done

function main () {
    execute "dotnet build $REPO_ROOT/src/Deploy"

    if [ -z "$pfs_tag" ]
    then
        echo "Searching for latest port-forwarding-web-api tag"
        pfs_tag="$(az acr repository show-tags --name vsclkonlinedev --repository port-forwarding-web-api --orderby time_desc --query "[?starts_with(@, '0')]|[0]" | sed s/\"//g)"
    fi

    if [ -z "$pfa_tag" ]
    then
        echo "Searching for latest port-forwarding-agent tag"
        pfa_tag="$(az acr repository show-tags --name vsclkonlinedev --repository port-forwarding-agent --orderby time_desc --query "[?starts_with(@, '1')]|[0]" | sed s/\"//g)"
    fi

    if [ -z "$portal_tag" ]
    then
        echo "Searching for latest vsclk.portal.website tag"
        portal_tag="$(az acr repository show-tags --name vsclkonlinedev --repository vsclk.portal.website --orderby time_desc --query "[?starts_with(@, '0')]|[0]" | sed s/\"//g)"
    fi

    if [ -f "terraform.tfstate" ]; then
        execute "mv terraform.tfstate bak.terraform.tfstate"
    fi

    execute "terraform init"

    if terraform workspace list | grep $user_alias > /dev/null; then
        execute "terraform workspace select $user_alias"
    else
        if [ -f "bak.terraform.tfstate" ]; then
            execute "terraform workspace new -state=bak.terraform.tfstate $user_alias"
        else
            execute "terraform workspace new $user_alias"
        fi

        execute "terraform init"
    fi

    execute "terraform apply -var 'alias=$user_alias' -var 'pfs_tag=$pfs_tag' -var 'pfa_tag=$pfa_tag' -var 'portal_tag=$portal_tag' -auto-approve"

    if [ -f "bak.terraform.tfstate" ]; then
        execute "mkdir -p $HOME/CEDev"
        execute "mv bak.terraform.tfstate $HOME/CEDev/$user_alias.terraform.tfstate"
    fi
}

main
