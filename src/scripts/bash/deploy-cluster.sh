#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Import utilities
# Script PIN 1753 to avoid global name conflicts
script_name_1753="$( basename "${BASH_SOURCE}" )"
script_dir_1753="$( cd "$( dirname "${BASH_SOURCE}" )" >/dev/null 2>&1 && pwd )"
. "${script_dir_1753}/utilities.sh"
echo_debug "Script 1753 name ${script_name_1753}"
echo_debug "Script 1753 dir ${script_dir_1753}"

# Initialize script parameters
add_option "subscription:" "s:"
subscription=""

add_option "prefix:" "p:"
prefix=""

add_option "name:" "n:"
name=""

add_option "env:" "e:"
env=""

add_option "instance:" "i:"
instance=""

add_option "stamp:" "m:"
stamp=""

add_option "location:" "l:"
location=""

add_option "validate-only"
validate_only=0

add_option "cluster-only"
cluster_only=0

add_option "team-group-name:"
team_group_name=

add_option "help" "h"
help=0
add_option "dry-run"
add_option "verbose" "v"
add_option "debug" "d"

# An option followed by a single colon ':' means that it *needs* an argument.
# An option followed by double colons '::' means that its argument is optional.
options="$(get_short_options)"
longoptions="$(get_long_options)"
parsed=$(getopt --options ${options} --longoptions ${longoptions} --name "$0" -- "$@")
if [[ $? -ne 0 ]]; then
    # e.g. $? == 1, then getopt has complained about wrong arguments to stdout
    exit 2
fi
# Use eval with "$parsed" to properly handle the quoting
# The set command sets the list of arguments equal to ${parsed}.
eval set -- "${parsed}"

dummy_arg="__dummy__"
extra_args=("${dummy_arg}") # Because set -u does not allow undefined variables to be used
discard_opts_after_doubledash=0 # 1=Discard, 0=Save opts after -- to ${extra_args}
while [[ ( ${discard_opts_after_doubledash} -eq 1 ) || ( $# -gt 0 ) ]]
do
    case "$1" in
        -s|--subscription) shift
            subscription="$1";;
        -p|--prefix) shift
            prefix="$1";;
        -n|--name) shift
            name="$1";;
        -e|--env) shift
            env="$1";;
        -i|--instance) shift
            instance="$1";;
        -m|--stamp) shift
            stamp="$1";;
        -l|--location) shift
            location="$1";;
        --team-group-name) shift
            team_group_name="$1";;
        --cluster-only) cluster_only=1;;
        --validate-only) validate_only=1;;
        --dry-run) set_dry_run 1;;
        -d|--debug) set_debug 1;;
        -v|--verbose) set_verbose 1;;
        -h|--help) help=1;;
        --) if [[ ${discard_opts_after_doubledash} -eq 1 ]]; then break; fi;;
        *) extra_args=("${extra_args[@]}" "$1");;
    esac
    shift # next argument
done

# Now delete the ${dummy_arg} from ${extra_args[@]} array # http://stackoverflow.com/a/16861932/1219634
extra_args=("${extra_args[@]/${dummy_arg}}")

# Debug output for parameters
echo_debug "Parameters: "
echo_debug "  short options: ${options}"
echo_debug "  long options: ${longoptions}"
echo_debug "  subscription: ${subscription}"
echo_debug "  prefix: ${prefix}"
echo_debug "  name: ${name}"
echo_debug "  env: ${env}"
echo_debug "  instance: ${instance}"
echo_debug "  stamp: ${stamp}"
echo_debug "  location: ${location}"
echo_debug "  team_group_name: ${team_group_name}"
echo_debug "  validate_only: ${validate_only}"
echo_debug "  cluster_only: ${cluster_only}"
echo_debug "  help: ${help}"
echo_debug "  dry_run: ${utilities_dry_run}"
echo_debug "  verbose: ${utilities_verbose}"
echo_debug "  debug: ${utilities_debug}"

if [ $help != 0 ] ; then
    echo "usage $(basename $0) [options]"
    echo " -s, --subscription  the subscription name or id [optional]"
    echo " -p, --prefix        the service name prefix, for example, 'vsclk'"
    echo " -n, --name          the service base name"
    echo " -e, --env           the service environment, 'dev', 'ppe', 'prod', 'rel'"
    echo " -i, --instance      the service instance name, defaults to env name"
    echo " -m, --stamp         the service stamp name"
    echo " -l, --location      the service primary location"
    echo "     --team-group-name   the dev team group display name"
    echo "     --generate-only generate ARM parameters only"
    echo "     --cluster-only  deploy only the cluster, not the environment or instance"
    echo "     --validate_only      validate_only deployment templates, do not deploy"
    echo "     --dry-run       do not create or deploye Azure resources"
    echo " -h, --help          show help"
    echo " -v, --verbose       emit verbose info"
    echo " -d, --debug         emit debug info"
    exit 0
fi

if [[ -z $prefix ]] ; then
    echo_error "Parameter --prefix is required" && exit 1
fi
if [[ -z $name ]] ; then
    echo_error "Parameter --name is required" && exit 1
fi
if [[ -z $env ]] ; then
    echo_error "Parameter --env is required" && exit 1
fi
if [[ -z $subscription ]] ; then
    subscription="${prefix}-${name}-${env}"
    echo_warning "Parameter --subscription is not specified, using '${subscription}'"
fi
if [[ -z $instance ]] ; then
    instance=$env
    echo_warning "Parameter --instance it not specified, using '${env}'"
fi
if [[ -z $stamp ]] ; then
    echo_error "Parameter --stamp is required" && exit 1
fi
if [[ -z $location ]] ; then
    echo_error "Parameter --location is required" && exit 1
fi
if [[ -z $team_group_name ]] ; then
    echo_error "Parameter --team-group-name is required" && exit 1
fi

function az_group_create()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local resource_group="${1}"
    local location="${2}"
    echo_info "Creating resource group '$resource_group' in '$location'"
    local az_group_create_command="az group create --name $resource_group --location $location --query id --output tsv"
    exec_dry_run "${az_group_create_command}" || return $?
}

function create_environment_keyvault()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local resource_group=${keyvault_rg}
    local vault_name=${keyvault_name}
    local enabledForDeployment="true"
    local enabledForTemplateDeployment="true"

    echo_info "Creating keyvault '${vault_name}'"
    local az_keyvault_create_command="az keyvault create --resource-group ${resource_group} --name ${vault_name} --location $location --enabled-for-deployment ${enabledForDeployment} --enabled-for-template-deployment ${enabledForTemplateDeployment} --query id --output tsv"
    exec_dry_run "${az_keyvault_create_command}" || return $?
}

function az_group_deployment_create()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local arm_template_name="${1}"
    local resource_group="${2}"
    local template_file="${template_dir}/${arm_template_name}.json"
    local paramters_file="$parameters_dir/${arm_template_name}.parameters.json"
    local az_command="az group deployment $deployment_verb --subscription ${subscription_id} --resource-group ${resource_group} --template-file ${template_file} --parameters @${paramters_file} --query id --output tsv"
    exec_dry_run $az_command
}

function init_cluster_envrionment_base_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
 
    # We always need the environment resource group and key vault before running ARM templates, 
    # so we have somewhere to pull parameter secrets from.
    # We also need the other resource groups in order to assign RBAC to service principals
    # that get created and added to the keyvault
    az_group_create $environment_rg $location || return $?
    az_group_create $instance_rg $location || return $?
    az_group_create $cluster_rg $location || return $?

    # Create the keyvault to store service principal attributes
    create_environment_keyvault || return $?

    # We need these service principals to exist and to be used by
    # deployment, or to be referenced by ARM parameters.
    ensure_service_principal "devops" || return $?
    az_role_assignment_create_scope_resource_group "devops" ${environment_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "devops" ${instance_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "devops" ${cluster_rg} "Contributor" || return $?

    ensure_service_principal "app" || return $?
    az_role_assignment_create_scope_resource_group "app" ${environment_rg} "Reader" || return $?
    az_role_assignment_create_scope_resource_group "app" ${instance_rg} "Reader" || return $?
    az_role_assignment_create_scope_resource_group "app" ${cluster_rg} "Reader" || return $?

    ensure_service_principal "cluster" || return $?
    az_role_assignment_create_scope_resource_group "cluster" ${cluster_rg} "Contributor" || return $?
}

function deploy_cluster_environment_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Deploying cluster environment ARM resources"
    az_group_deployment_create "cluster-environment" "${environment_rg}"
}

function deploy_cluster_instance_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Deploying cluster instance ARM resources"
    az_group_deployment_create "cluster-instance" "${instance_rg}"
}

function deploy_cluster_stamp_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Deploying cluster stamp ARM resources"
    az_group_deployment_create "cluster-stamp" "${cluster_rg}"
}

function az_keyvault_secret_set()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local name="${1}"
    local value="${2}"
    local expires="$(set +u; echo ${3})"

    local az_command="az keyvault secret set --vault-name $keyvault_name --name $name --value $value --query id --output tsv"

    if [ ! -z $expires ] ; then
        az_command="${az_command} --expires $expires"
    fi

    exec_dry_run $az_command
}

function az_keyvault_secret_exists()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local name="${1}"
    local az_command="az keyvault secret show --vault-name $keyvault_name --name $name"
    get_dry_run && return 1 # doesn't exist for dry-run
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_keyvault_secret_set_if_unset()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local name="${1}"
    local value="${2}"
    local expires="$(set +u; echo "${3}")"

    if ! az_keyvault_secret_exists $name; then
        az_keyvault_secret_set $name $value $expires
    fi
}

function get_sp_name()
{
    local base_name="${1}"
    echo "${environment_name}-${base_name}-sp"
}

function jq_json64()
{
    local json64="${1}"
    local json=$(echo $json64 | base64 -d)
    local query="${2}"
    echo $json | jq ${query} | tr -d '"'
}

function az_ad_sp_exists()
{
    local sp_name="$1"
    local az_ad_sp_show_command="az ad sp show --id http://${sp_name}"
    get_dry_run && return 1 # doesn't exist for dry-run
    exec_dry_run "${az_ad_sp_show_command}" 2> /dev/null > /dev/null
}

function az_ad_sp_show_json()
{
    local sp_name="$1"
    local az_command="az ad sp show --id http://${sp_name} --output json"
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

function az_ad_sp_create_for_rbac()
{
    local sp_name="$1"
    local password="$2"
    local az_command="az ad sp create-for-rbac --name http://${sp_name} --password ${password} --skip-assignment -o json"
    exec_dry_run "${az_command}" > /dev/null 
}


function ensure_service_principal()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local base_name="${1}"
    local secret_base_name="${base_name}-sp"
    local sp_name="$(get_sp_name ${base_name})" || return $?
    local sp=

    if ! az_ad_sp_exists $sp_name ; then
        echo_info "Creating service principal '${sp_name}'"
        local passwordLength=128
        local password=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w ${passwordLength} | head -n 1)
        local expires="$(date -u -d "+1 year" -I)T00:00:00Z"
        az_ad_sp_create_for_rbac $sp_name $password || return $?
        local set_secret=az_keyvault_secret_set
    else
        echo_verbose "Service principal '${sp_name}' already exists."
        local password=
        local expires=
        local set_secret=az_keyvault_secret_set_if_unset
    fi

    echo_verbose "Getting service principal '${sp_name}' attributes"
    sp=$(az_ad_sp_show_json $sp_name) || return $?
    echo_debug "$sp"

    local sp_json64=$(echo $sp | base64 --wrap=0) || return $?

    local spid=$(jq_json64 $sp_json64 .objectId) || return $?
    echo_debug "service principal spid:     ${spid}"

    local appid=$(jq_json64 $sp_json64 .appId) || return $?
    echo_debug "service principal appid:    ${appid}"

    local name=$(jq_json64 $sp_json64 .servicePrincipalNames[0]) || return $?
    echo_debug "service principal name:     ${name}"

    local tenant=$(jq_json64 $sp_json64 .appOwnerTenantId) || return $?
    echo_debug "service principal tenant:   ${tenant}"

    echo_debug "service principal password: ${password}"
    echo_debug "service principal expires:  ${expires}"

    # ensure required secrets are set in the keyvault for this sp
    echo_info "Updating keyvault properties for service principal '${sp_name}'"
    ${set_secret} "${secret_base_name}-id" "$spid" || return $?
    ${set_secret} "${secret_base_name}-appid" "$appid" || return $?
    ${set_secret} "${secret_base_name}-name" "$name" || return $?
    ${set_secret} "${secret_base_name}-tenant" "$tenant" || return $?
    if [ ! -z $password ]; then
        ${set_secret} "${secret_base_name}-password" $password $expires || return $?
    fi

    # track the object ids for later
    echo_verbose "Setting principal_objectids and principal_appids"
    principal_objectids[${sp_name}]=$spid
    echo_debug "Service principal id ${sp_name} : ${principal_objectids[$sp_name]}"
    principal_appids[${sp_name}]=$appid
    echo_debug "Service principal appid ${sp_name} : ${principal_appids[$sp_name]}"
}

function az_role_assignment_create_scope_resource_group()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local base_name="${1}"
    local resource_group="${2}"
    local role="${3}"

    local sp_name="$(get_sp_name ${base_name})"
    local appId="${principal_appids[$sp_name]}"
    local scope="/subscriptions/${subscription_id}/resourceGroups/${resource_group}"
    local az_command="az role assignment create --assignee ${appId} --scope $scope --role ${role} --query id --output tsv"

    echo_info "Granting role '${role}' to service principal '${sp_name}' for scope '${scope}'"
    exec_dry_run "${az_command}"
    # ignore assignment errors; could already be set
    return 0
}

function az_aks_get_credentials()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Getting the AKS credentials for ${cluster_name}"
    local cluster_name="${1}"
    local cluster_rg="${2}"
    local az_command="az aks get-credentials --resource-group $cluster_rg --name $cluster_name"
    exec_dry_run $az_command
}

function az_account_set()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"

    echo_info "Setting the azure subscription to $subscription"
    exec_dry_run az account set -s $subscription || return $?

    if get_dry_run ; then
        subscription_id="dry-run-subscription-id"
        subscription_name="dry-run-subscription-name" 
    else
        local account=$(az account show --output json) || return $?
        subscription_name=$(echo $account | jq .name | tr -d '"')
        subscription_id=$(echo $account | jq .id | tr -d '"')
    fi
    echo_info "Azure subscription: $subscription_name [$subscription_id]"
}

function kubectl_apply()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local file="${1}"
    local kubectl_command="kubectl apply -f $file"
    exec_dry_run $kubectl_command
}

function generate_parameters()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"

    echo_info "Generating cluster parameters files"

    # parameters require object ids
    set_ad_object_ids || return $?
    
    exec_verbose . "${script_dir_1753}/generate-cluster-parameters.sh" \
        --subscription-id $subscription_id \
        --prefix $prefix \
        --name $name \
        --env $env \
        --instance $instance \
        --stamp $stamp \
        --keyvault-resource-group $keyvault_rg \
        --keyvault-name $keyvault_name \
        --app-sp-id $app_sp_objectid \
        --devops-sp-id $devops_sp_objectid \
        --team-id $team_objectid \
        --user-id $user_objectid
}

function az_ad_signed_in_user_object_id()
{
    if get_dry_run; then
        echo "dry-run-user"
        return 0
    fi

    az ad signed-in-user show --query objectId -o tsv
}

function az_ad_group_objectid()
{
    if get_dry_run; then
        echo "dry-run-group"
        return 0
    fi
    local group_name="${1}"
    local id="$(az ad group show -g ${group_name} --query objectId -o tsv)"
    if [ -z $id ]; then
        echo_error "Could not find AD group '$group_name'"
        return 1
    fi
    echo "${id}"
}

function set_ad_object_ids()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"

    echo_verbose "getting user object id"
    user_objectid="$(az_ad_signed_in_user_object_id)" || return $?
    echo_debug "user_objectid: ${user_objectid}"

    echo_verbose "getting team group object id"
    team_objectid="$(az_ad_group_objectid ${team_group_name})" || return $?
    echo_debug "team_objectid: ${team_objectid}"

    echo_verbose "getting app service principal object id"
    local sp_name=$(get_sp_name "app")
    app_sp_objectid="${principal_objectids[$sp_name]}"
    echo_debug "app_sp_objectid: ${app_sp_objectid}"

    echo_verbose "getting devops service principal object id"
    local sp_name=$(get_sp_name "devops")
    devops_sp_objectid="${principal_objectids[$sp_name]}"
    echo_debug "devops_sp_objectid: ${devops_sp_objectid}"
}

# Script Variables
src_dir="$( cd "${script_dir_1753}/../.." >/dev/null 2>&1 && pwd )"
template_dir="${src_dir}/arm"
parameters_dir="${template_dir}/parameters"
k8s_dir="${src_dir}/k8s"
charts_dir="${src_dir}/charts"
environment_name="${prefix}-${name}-${env}"
instance_name="${prefix}-${name}-${env}-${instance}"
environment_rg="${environment_name}"
instance_rg="${instance_name}"
keyvault_rg="${environment_rg}"
keyvault_name="${environment_name}-kv"
cluster_rg="${instance_rg}-${stamp}"
cluster_name="${cluster_rg}-cluster"
declare -A principal_objectids=()
declare -A principal_appids=()
# Initialize the verb to use for deployment
deployment_action="Creating"
deployment_verb="create"
if [ $validate_only != 0 ] ; then
    deployment_action="Validating"
    deployment_verb="validate"
fi

echo_debug "Script variables:"
echo_debug "  script_dir_1753: ${script_dir_1753}"
echo_debug "  template_dir: ${template_dir}"
echo_debug "  parameters_dir: ${parameters_dir}"
echo_debug "  k8s_dir: ${k8s_dir}"
echo_debug "  charts_dir: ${charts_dir}"
echo_debug "  environment_name: ${environment_name}"
echo_debug "  instance_name: ${instance_name}"
echo_debug "  environment_rg: ${environment_rg}"
echo_debug "  instance_rg: ${instance_rg}"
echo_debug "  keyvault_rg: ${keyvault_rg}"
echo_debug "  keyvault_name: ${keyvault_name}"
echo_debug "  cluster_rg: ${cluster_rg}"
echo_debug "  cluster_name: ${cluster_name}"
echo_debug "  deployment_action: $deployment_action"
echo_debug "  deployment_verb: $deployment_verb"

function main()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"

    # Set the Azure subscription
    az_account_set || return $?

    if [ ! $cluster_only -eq 1 ] ; then
        # Everything depends on the base resoruce and service principals
        init_cluster_envrionment_base_resources || return $?

        # Generate ARM parameters files
        generate_parameters || return $?

        # Deploy environment and instance ARM templates
        echo_verbose "Deploy cluster environment"
        deploy_cluster_environment_resources || return $?
        echo_verbose "Deploy cluster instance"
        deploy_cluster_instance_resources || return $? 
        echo_verbose "Deploy the cluster to a stamp ($location)"
        deploy_cluster_stamp_resources || return $?
        if [ $validate_only -eq 1 ]; then
            return 0
        fi
    fi

    # Connect kubectl to the AKS cluster
    az_aks_get_credentials $cluster_name $cluster_rg || return $?
    exec_dry_run kubectl cluster-info || return $?

    echo_info "Applying K8s default cluster configuration"
    kubectl_apply "$k8s_dir/custom-default-psp.yml" || return $?
    kubectl_apply "$k8s_dir/custom-default-psp-role.yml" || return $?
    kubectl_apply "$k8s_dir/custom-default-psp-rolebinding.yml" || return $?
    kubectl_apply "$k8s_dir/kubernetes-dashboard-clusterrolebinding.yml" || return $?
    kubectl_apply "$k8s_dir/nginx-ingress-clusterrole.yml" || return $?
    kubectl_apply "$k8s_dir/nginx-ingress-clusterrolebinding.yml" || return $?
    kubectl_apply "$k8s_dir/tiller-sa.yml" || return $?
    kubectl_apply "$k8s_dir/tiller-sa-clusterrolebinding.yml" || return $?

    echo_info "Installing helm-tiller to system namespace"
    exec_dry_run helm init --service-account "tiller-sa" --wait || return $?

    echo_info "Installing nginx with load balancer"
    local install_name="nginx-ingress"
    # TODO: the --replace switch is documented not safe for production
    exec_dry_run helm install "$charts_dir/nginx-ingress" --name ${install_name} --replace --wait

    # TODO
    # - Traffic manager endpoints
    # - DNS names
    # See [authenticate-with-acr](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-aks?toc=%2Fen-us%2Fazure%2Faks%2FTOC.json&bc=%2Fen-us%2Fazure%2Fbread%2Ftoc.json).
    # See [control-kubeconfig-access](https://docs.microsoft.com/en-us/azure/aks/control-kubeconfig-access).
}

main
