#!/bin/bash
# renormalize

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Import utilities
script_dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
. "${script_dir}/utilities.sh"

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

add_option "location:" "l:"
location=""

add_option "generate-only"
generate_only=0

add_option "validate-only"
validate_only=0

add_option "cluster-only"
cluster_only=0

add_option "keyvault-reader-object-id:"
keyvault_reader_objectid=

add_option "keyvault-reader-tenant-id:"
keyvault_reader_tenantid=

add_option "help" "h"
help=0
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
        -l|--location) shift
            location="$1";;
        --keyvault-reader-object-id) shift
            keyvault_reader_objectid="$1";;
        --keyvault-reader-tenant-id) shift
            keyvault_reader_tenantid="$1";;
        --generate-only) generate_only=1;;
        --cluster-only) cluster_only=1;;
        --validate-only) validate_only=1;;
        -h|--help) help=1;;
        -d|--debug) set_debug 1;;
        -v|--verbose) set_verbose 1;;
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
echo_debug "  location: ${location}"
echo_debug "  keyvault_reader_objectid: ${keyvault_reader_objectid}"
echo_debug "  keyvault_reader_tenantid: ${keyvault_reader_tenantid}"
echo_debug "  generate_only: ${generate_only}"
echo_debug "  validate_only: ${validate_only}"
echo_debug "  cluster_only: ${cluster_only}"
echo_debug "  help: ${help}"
echo_debug "  verbose: ${verbose}"
echo_debug "  debug: ${debug}"

if [ $help != 0 ] ; then
    echo "usage $(basename $0) [options]"
    echo " -s, --subscription  the subscription name or id [optional]"
    echo " -p, --prefix        the service name prefix, for example, 'vsclk'"
    echo " -n, --name          the service base name"
    echo " -e, --env           the service environment, 'dev', 'ppe', 'prod', 'rel'"
    echo " -i, --instance      the service instance name, defaults to env name"
    echo " -l, --location      the service primary location"
    echo "     --keyvault-reader-object-id"
    echo "                        the keyvault reader service principal object id"
    echo "     --keyvault-reader-tenant-id"
    echo "                        the keyvault reader service principal tenant id"
    echo "     --generate-only generate ARM parameters only"
    echo "     --cluster-only  deploy only the regional cluster, not the environment or instance"
    echo "     --validate_only      validate_only deployment templates, do not deploy"
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
if [[ -z $location ]] ; then
    echo_error "Parameter location is required" && exit 1
fi
if [[ -z ${keyvault_reader_objectid} ]] ; then
    echo_error "Parameter --keyvault-reader-object-id is required" && exit 1
fi
if [[ -z ${keyvault_reader_tenantid} ]] ; then
    echo_error "Parameter --keyvault-reader-tenant-id is required" && exit 1
fi

function create_resource_group()
{
    echo_debug "$0::${FUNCNAME[0]}"
    local RG="${1}"
    local location="${2}"
    echo "Creating resource group '$RG' in '$location'"
    local STATE=$(az group show -n $RG --query properties.provisioningState -o tsv 2>/dev/null)
    if [[ $STATE != "Succeeded" ]] ; then
        az group create -n $RG -l $location >/dev/null || return $?
    fi
}

function create_environment_resource_group()
{
    echo_debug "$0::${FUNCNAME[0]}"
    create_resource_group $environment_rg $location
}

function create_instance_resource_group()
{
    echo_debug "$0::${FUNCNAME[0]}"
    create_resource_group $instance_rg $location
}

function deploy_cluster_environment_resources()
{
    echo_debug "$0::${FUNCNAME[0]}"
    create_environment_resource_group
    local arm_template_name="cluster-environment"
    local template_file="${template_dir}/${arm_template_name}.json"
    local paramters_file="$parameters_dir/${arm_template_name}.parameters.json"
    echo "${deployment_action} ${arm_template_name}"
    az group deployment $deployment_verb \
        --subscription "$subscription_id" \
        --resource-group "$environment_rg" \
        --template-file "${template_file}" \
        --parameters @"${paramters_file}" \
        --output "jsonc"
}

function deploy_cluster_instance_resources()
{
    echo_debug "$0::${FUNCNAME[0]}"
    create_instance_resource_group
    local arm_template_name="cluster-instance"
    local template_file="${template_dir}/${arm_template_name}.json"
    local paramters_file="$parameters_dir/${arm_template_name}.parameters.json"
    echo "${deployment_action} ${arm_template_name}"
    az group deployment $deployment_verb \
        --subscription "$subscription_id" \
        --resource-group "${instance_rg}" \
        --template-file "${template_file}" \
        --parameters @"${paramters_file}" \
        --output "jsonc"
}

function deploy_cluster_resources()
{
    echo_debug "$0::${FUNCNAME[0]}"
    local arm_template_name="cluster-region"
    local template_file="${template_dir}/${arm_template_name}.json"
    local paramters_file="$parameters_dir/${arm_template_name}.parameters.json"
    echo "${deployment_action} ${arm_template_name}"
    az group deployment $deployment_verb \
        --subscription "$subscription_id" \
        --resource-group "${instance_rg}" \
        --template-file "${template_file}" \
        --parameters @"${paramters_file}" \
        --output "jsonc"
}

function set_keyvault_secret()
{
    echo_debug "$0::${FUNCNAME[0]}"
    local name="${1}"
    local value="${2}"
    local expires="${3}"

    local az_command="az keyvault secret set --vault-name $keyvault_name --name $name --value $value"

    if [ ! -z $expires ] ; then
        az_command="$az_command --expires $expires"
    fi

    echo_verbose "$az_command"
    $az_command
}

function create_cluster_sp()
{
    echo_debug "$0::${FUNCNAME[0]}"
    echo_verbose "Checking sp $cluster_sp_name"
    if [[ ! $(az ad sp show --id "http://$cluster_sp_name" -o json) ]] ; then
        echo_verbose "Creating sp $cluster_sp_name"
        local passwordLength=128
        local password=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w $passwordLength | head -n 1)
        local sp=$(az ad sp create-for-rbac --name $cluster_sp_name --password $password --skip-assignment -o json) || return $?
        echo_verbose "created sp: $sp"
        local appid=$(echo $sp | jq .appId | tr -d '"')
        local name=$(echo $sp | jq .name | tr -d '"')
        local tenant=$(echo $sp | jq .tenant  | tr -d '"')
        local expires="$(date -u -d "+1 year" -I)T00:00:00Z"
        local sp=$(az ad sp show --id "http://$cluster_sp_name" -o json) || return $?
        local spid=$(echo $sp | jq .objectId  | tr -d '"')
        set_keyvault_secret "cluster-sp-id" $spid "" || return $?
        set_keyvault_secret "cluster-sp-appid" $appid "" || return $?
        set_keyvault_secret "cluster-sp-name" $name "" || return $?
        set_keyvault_secret "cluster-sp-tenant" $tenant "" || return $?
        set_keyvault_secret "cluster-sp-password" $password $expires || return $?
    fi
}

function assign_cluster_sp_roles()
{
    echo_debug "$0::${FUNCNAME[0]}"
    local sp=$(az ad sp show --id "http://$cluster_sp_name" -o json) || return $?
    local appid=$(echo $sp | jq .appId  | tr -d '"')
    local scope="/subscriptions/$subscription/resourceGroups/$instance_rg"
    local role="Contributor"
    local az_command="az role assignment create --assignee $appid --scope $scope --role $role"
    echo_verbose "$az_command"
    $az_command
    return 0
}

function cluster_info()
{
    echo_debug "$0::${FUNCNAME[0]}"
    kubectl cluster-info
}

function connect_to_aks()
{
    echo_debug "$0::${FUNCNAME[0]}"
    local cluster_name="${1}"
    local cluster_rg="${2}"
    az aks get-credentials --resource-group $cluster_rg --name $cluster_name
}

function set_az_subscription()
{
    echo_debug "$0::${FUNCNAME[0]}"

    if [[ -z $subscription ]] ; then
        echo_error "Subscription is not set."
        return 1
    fi

    az account set -s $subscription >/dev/null || return $?
    subscription_name=$(az account show --query name -o tsv) || return $?
    subscription_id=$(az account show --query id -o tsv) || return $?
    
    echo_verbose "Subscription: $subscription_name [$subscription_id]"
}

function kubectl_apply()
{
    echo_debug "$0::${FUNCNAME[0]}"

    local file="${1}"
    local kubectl_command="kubectl apply -f $file"
    echo_verbose "$kubectl_command"
    $kubectl_command
}

function generate_parameters()
{
    echo_debug "$0::${FUNCNAME[0]}"

    # Generate parameters
    local params=""
    get_debug && params="${params} -d"
    get_verbose && params="${params} -v"
    echo_verbose "calling generate-cluster-parameters.sh ${params} ..."
    . "generate-cluster-parameters.sh" ${params} \
        --subscription-id $subscription_id \
        --prefix $prefix \
        --name $name \
        --env $env \
        --instance $instance \
        --keyvault-resource-group $keyvault_rg \
        --keyvault-name $keyvault_name \
        --keyvault-reader-object-id $keyvault_reader_objectid \
        --keyvault-reader-tenant-id $keyvault_reader_tenantid \
        || return $?
}

# Script Variables
src_dir="$( cd "${script_dir}/../.." >/dev/null 2>&1 && pwd )"
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
cluster_sp_name="${environment_name}-cluster-sp"
cluster_rg="${instance_rg}"
cluster_name="${instance_name}-cluster"
# Initialize the verb to use for deployment
deployment_action="Creating"
deployment_verb="create"
if [ $validate_only != 0 ] ; then
    deployment_action="Validating"
    deployment_verb="validate"
fi

echo_debug "Script variables:"
echo_debug "  script_dir: ${script_dir}"
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
echo_debug "  cluster_sp_name: ${cluster_sp_name}"
echo_debug "  cluster_rg: ${cluster_rg}"
echo_debug "  cluster_name: ${cluster_name}"
echo_debug "  deployment_action: $deployment_action"
echo_debug "  deployment_verb: $deployment_verb"

function main()
{
    echo_debug "$0::${FUNCNAME[0]}"

    set_az_subscription || return $?

    generate_parameters || return $?
    if [ $generate_only -eq 1 ]; then
        return 0
    fi

    # Deploy base resources
    if [ ! $cluster_only -eq 1 ] ; then
        deploy_cluster_environment_resources || return $?
        deploy_cluster_instance_resources || return $? 
    fi
    if [ $validate_only -eq 1 ]; then
        return 0
    fi

    # Create the cluster service principal and store in key vault
    create_cluster_sp || return $?
    assign_cluster_sp_roles || return $?

    # Deploy the cluster to a single region for now -- into the instance rg
    deploy_cluster_resources || return $?

    # Connect to the cluster
    connect_to_aks $cluster_name $cluster_rg || return $?
    cluster_info || return $?

    # Apply RBAC and default pod security policy
    kubectl_apply "$k8s_dir/custom-default-psp.yml" || return $?
    kubectl_apply "$k8s_dir/custom-default-psp-role.yml" || return $?
    kubectl_apply "$k8s_dir/custom-default-psp-rolebinding.yml" || return $?
    kubectl_apply "$k8s_dir/nginx-ingress-clusterrole.yml" || return $?
    kubectl_apply "$k8s_dir/nginx-ingress-clusterrolebinding.yml" || return $?
    kubectl_apply "$k8s_dir/tiller-sa.yml" || return $?
    kubectl_apply "$k8s_dir/tiller-sa-clusterrolebinding.yml" || return $?

    # Install helm-tiller to system namespace
    echo_verbose "Installing tiller"
    helm init --service-account "tiller-sa" --wait || return $?

    # Install nginx with load balancer
    echo_verbose "Installing nginx-ingress"
    helm install "$charts_dir/nginx-ingress" --name "nginx-ingress" --wait

    # TODO
    # - Traffic manager endpoints
    # - DNS names
    # See [authenticate-with-acr](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-aks?toc=%2Fen-us%2Fazure%2Faks%2FTOC.json&bc=%2Fen-us%2Fazure%2Fbread%2Ftoc.json).
    # See [control-kubeconfig-access](https://docs.microsoft.com/en-us/azure/aks/control-kubeconfig-access).

}

main
