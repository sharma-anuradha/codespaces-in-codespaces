#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Import utilities
# Script PIN 2998 to avoid global name conflicts
script_name_2998="$( basename "${BASH_SOURCE}" )"
script_dir_2998="$( cd "$( dirname "${BASH_SOURCE}" )" >/dev/null 2>&1 && pwd )"
. "${script_dir_2998}/utilities.sh"
set_dry_run 0
set_verbose 0
set_debug 0
echo_debug "Script 2998 name ${script_name_2998}"
echo_debug "Script 2998 dir ${script_dir_2998}"

function az_ad_sp_exists()
{
    echo_debug "${script_name_2998}::${FUNCNAME[0]} $@"
    local id="$1"
    local command="az ad sp show --id $id"
    if get_dry_run; then
        echo_verbose "${command}"
        return 1
    fi
    exec_dry_run $command 2> /dev/null > /dev/null
}

function az_ad_app_exists()
{
    echo_debug "${script_name_2998}::${FUNCNAME[0]} $@"
    local appId="$1"
    local command="az ad app show --id $appId"
    if get_dry_run; then
        echo_verbose "${command}"
        return 1
    fi
    exec_dry_run "${command}" 2> /dev/null > /dev/null
}

function az_ad_sp_delete()
{
    echo_debug "${script_name_2998}::${FUNCNAME[0]} $@"
    local id="$1"
    local command="az ad sp delete --id $id"
    exec_dry_run $command
}

function az_ad_app_delete()
{
    echo_debug "${script_name_2998}::${FUNCNAME[0]} $@"
    local appId="$1"
    local command="az ad app delete --id $appId"
    exec_dry_run $command
}

function az_group_delete()
{
    echo_debug "${script_name_2998}::${FUNCNAME[0]} $@"
    local group="$1"
    local command="az group delete --name $group --no-wait --yes"
    exec_dry_run $command
}

function delete_apps_and_service_principals()
{
    echo_debug "${script_name_2998}::${FUNCNAME[0]} $@"
    local displayNamePrefix="$1"
    local sp_command="az ad sp list --display-name ${displayNamePrefix} --output json"
    local app_command="az ad app list --display-name ${displayNamePrefix} --output json"

    echo_info "Deleting service principals for ${displayNamePrefix}"
    echo_verbose "${sp_command}"
    if sps=$(${sp_command}); then
        echo_debug "sps: ${sps}"
        for i in {0..4}; do
            local sp="$(echo $sps | jq -M .[$i])"
            echo_debug "sp: $sp"
            [ "$sp" == "null" ] && continue

            local displayName="$(echo $sp | jq -M .displayName | tr -d '"')"
            local id="$(echo $sp | jq -M .objectId | tr -d '"')"
            [ "$id" == "null" ] && continue

            echo_info "Deleting service principal: $displayName [$id]"
            if az_ad_sp_exists $id; then 
                az_ad_sp_delete $id
            fi
        done
    else
        local result="$?"
        echo_info "No service principals found: $result"
    fi

    echo_info "Deleting applications for ${displayNamePrefix}"
    echo_verbose "${app_command}"
    if apps=$(${app_command}); then
        echo_debug "apps: ${apps}"
        for i in {0..4}; do
            local app="$(echo $apps | jq -M .[$i])"
            echo_debug "app: $app"
            [ "$app" == "null" ] && continue

            local displayName="$(echo $sp | jq -M .displayName | tr -d '"')"
            local appId="$(echo $sp | jq -M .appId | tr -d '"')"
            [ "$appId" == "null" ] && continue

            echo_info "Deleting application: $displayName [$appId]"
            if az_ad_app_exists $appId; then
                az_ad_app_delete $appId || return $?
            fi
        done
    else
        local result="$?"
        echo_info "No applications found: $result"
    fi

    return 0
}

function az_group_show_prefix_json()
{
    local displayNamePrefix="$1"
    local command="az group list --query [?starts_with(name,'vsclk-sample')].{name:name,state:properties.provisioningState} --output json"
    ${command}
}

function delete_resource_groups()
{
    echo_debug "${script_name_2998}::${FUNCNAME[0]} $@"
    local displayNamePrefix="$1"
    if groups=$(az_group_show_prefix_json $displayNamePrefix); then
        echo_info "Groups to delete"
        echo "${groups}"
        for i in {0..4}; do
            local group="$(echo $groups | jq -M .[$i])"
            echo_debug "group: $group"
            [ "$group" == "null" ] && continue

            local name="$(echo $group | jq -M .name | tr -d '"')"

            echo_info "Deleting resource group: $name"
            az_group_delete $name
        done
    fi
    return 0
}

displayNamePrefix="$(set +u; echo "$1")"
if [ -z $displayNamePrefix ]; then
    echo_error "${script_name_2998}: displayNamePrefix required"
    exit 1
fi

delete_apps_and_service_principals $displayNamePrefix
delete_resource_groups $displayNamePrefix
