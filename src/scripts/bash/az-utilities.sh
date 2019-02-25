#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Import utilities
# Script PIN 7501 to avoid global name conflicts
script_name_7501="$( basename "${BASH_SOURCE}" )"
script_dir_7501="$( cd "$( dirname "${BASH_SOURCE}" )" >/dev/null 2>&1 && pwd )"
. "${script_dir_7501}/utilities.sh"
echo_debug "Script 7501 name ${script_name_7501}"
echo_debug "Script 7501 dir ${script_dir_7501}"

az_args_output_id="--query id --output tsv"
az_args_output_objectid="--query objectId --output tsv"
az_args_output_json="--output json"

function az_account_set()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local subscription="$1"
    echo_info "Setting the azure subscription to '$subscription'"
    local az_command="az account set -s $subscription"
    exec_dry_run ${az_command}
}

function az_account_show_json()
{
    local az_command=$(az account show $az_args_output_json)
    if get_dry_run ; then
        echo "{
            \"id\": \"dry-run-subscription-id\",
            \"name\": \"dry-run-subscription-name\"
        }"
    else
        ${az_command}
    fi
}

function az_ad_app_delete()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local appId="$1"
    local command="az ad app delete --id $appId"
    exec_dry_run $command
}

function az_ad_app_exists()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local appId="$1"
    local command="az ad app show --id $appId"
    if get_dry_run; then
        echo_verbose "${command}"
        return 1
    fi
    exec_dry_run "${command}" 2> /dev/null > /dev/null
}

function az_ad_group_objectid()
{
    local group_name="${1}"
    if get_dry_run; then
        echo "dry-run-${group_name}-id"
        return 0
    fi
    local id="$(az ad group show -g ${group_name} $az_args_output_objectid)"
    if [ -z $id ]; then
        echo_error "Could not find AD group '$group_name'"
        return 1
    fi
    echo "${id}"
}

function az_ad_signed_in_user_object_id()
{
    local az_command="az ad signed-in-user show $az_args_output_objectid"
    if get_dry_run; then
        echo "dry-run-user-id"
        return 0
    fi
    ${az_command}
}

function az_ad_sp_create_for_rbac()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local sp_name="$1"
    local password="$2"
    local az_command="az ad sp create-for-rbac --name http://${sp_name} --password ${password} --skip-assignment $az_args_output_id"
    exec_dry_run ${az_command}
}

function az_ad_sp_delete()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local id="$1"
    local command="az ad sp delete --id $id"
    exec_dry_run $command
}

function az_ad_sp_exists()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local sp_name="$1"
    local az_command="az ad sp show --id http://${sp_name}"
    get_dry_run && return 1 # doesn't exist for dry-run
    exec_dry_run ${az_command} 2> /dev/null > /dev/null
}

function az_ad_sp_show_json()
{
    local sp_name="$1"
    local az_command="az ad sp show --id http://${sp_name} $az_args_output_json"
    if get_dry_run; then
        echo "{
            \"appDisplayName\": \"${sp_name}\",
            \"appId\": \"dry-run-appid\",
            \"appOwnerTenantId\": \"dry-run-tenant\",
            \"objectId\": \"dry-run-objectid\",
            \"servicePrincipalNames\": [
                \"http://${sp_name}\"
            ]
        }"
    else
        ${az_command}
    fi
}

function az_aks_browse()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local cluster_rg="$1"
    local cluster_name="$2"
    echo_info "Browsing AKS cluster '${cluster_name}'"
    local az_command="az aks browse --resource-group $cluster_rg --name $cluster_name"
    exec_dry_run ${az_command}
}

function az_aks_get_credentials()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local cluster_rg="${1}"
    local cluster_name="${2}"
    echo_info "Getting the AKS credentials for cluster '${cluster_name}'"
    local az_command="az aks get-credentials --resource-group $cluster_rg --name $cluster_name"
    exec_dry_run ${az_command}
}

function az_group_create()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local resource_group="${1}"
    local location="${2}"
    echo_info "Creating resource group '$resource_group' in '$location'"
    local az_group_create_command="az group create --name $resource_group --location $location $az_args_output_id"
    exec_dry_run "${az_group_create_command}" || return $?
}

function az_group_delete()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local group="$1"
    local command="az group delete --name $group --no-wait --yes"
    exec_dry_run $command
}

function az_group_deployment_create()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local resource_group="${1}"
    local template_file="${2}"
    local paramters_file="${3}"
    local az_command="az group deployment create --resource-group ${resource_group} --template-file ${template_file} --parameters @${paramters_file} $az_args_output_id"
    exec_dry_run ${az_command}
}

function az_group_deployment_validate()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local resource_group="${1}"
    local template_file="${2}"
    local paramters_file="${3}"
    local az_command="az group deployment validate --resource-group ${resource_group} --template-file ${template_file} --parameters @${paramters_file} $az_args_output_id"
    exec_dry_run ${az_command}
}

function az_keyvault_create()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local resource_group="${1}"
    local keyvault_name="${2}"
    local enabledForDeployment="true"
    local enabledForTemplateDeployment="true"
    echo_info "Creating keyvault '${keyvault_name}'"
    local az_command="az keyvault create --resource-group ${resource_group} --name ${keyvault_name} --location $location --enabled-for-deployment ${enabledForDeployment} --enabled-for-template-deployment ${enabledForTemplateDeployment} $az_args_output_id"
    exec_dry_run ${az_command}
}

function az_keyvault_secret_exists()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local keyvault_name="${1}"
    local name="${2}"
    local az_command="az keyvault secret show --vault-name $keyvault_name --name $name"
    get_dry_run && return 1 # doesn't exist for dry-run
    exec_dry_run ${az_command} 2> /dev/null > /dev/null
}

function az_keyvault_secret_set()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local keyvault_name="${1}"
    local name="${2}"
    local value="${3}"
    local expires="$(set +u; echo ${4})"

    local az_command="az keyvault secret set --vault-name $keyvault_name --name $name --value $value $az_args_output_id"

    if [ ! -z $expires ] ; then
        az_command="${az_command} --expires $expires"
    fi

    exec_dry_run ${az_command}
}

function az_keyvault_secret_set_if_unset()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local keyvault_name="${1}"
    local name="${2}"
    local value="${3}"
    local expires="$(set +u; echo ${4})"

    if ! az_keyvault_secret_exists $keyvault_name $name; then
        az_keyvault_secret_set $keyvault_name $name $value $expires
    fi
}

function az_role_assignment_create_scope_resource_group()
{
    echo_debug "${script_name_7501}::${FUNCNAME[0]} $@"
    local subscription_id="${1}"
    local resource_group="${3}"
    local scope="/subscriptions/${subscription_id}/resourceGroups/${resource_group}"
    local appid="${2}"
    local role="${4}"
    local az_command="az role assignment create --assignee ${appid} --scope $scope --role ${role} $az_args_output_id"
    echo_info "Granting role '${role}' to application id '${appid}' for scope '${scope}'"
    exec_dry_run ${az_command}
}
