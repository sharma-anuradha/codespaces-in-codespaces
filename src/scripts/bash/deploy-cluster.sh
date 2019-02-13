#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

function echo_error()
{
    echo -e "\e[01;31mError: ${1}\e[0m" >&2
}

function echo_verbose()
{
    if [ $verbose != 0 ] ; then
        echo -e "\e[01;33mVERBOSE: ${1}\e[0m"
    fi
}

# Initialize script parameters
help=0      # --help, -h
debug=0     # --debug, -d
verbose=0   # --verbose, -v
validate=0  # --validate
cluster_only=0 # --cluster_only
subscription="" # --subscription, -s
prefix=""   # --prefix, -p
servicename=""  # --servicename, -n
env=""      # --env, -e
instance="" # --instance, -i
location="" # --location, -l
# An option followed by a single colon ':' means that it *needs* an argument.
# An option followed by double colons '::' means that its argument is optional.
options="hdvp:n:e:i:l:s:"
longoptions="help,debug,verbose,validate,cluster-only,subscription:,prefix:,servicename:,env:,instance:,location:"
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
        -h|--help) help=1;;
        -d|--debug) debug=1;;
        -v|--verbose) verbose=1;;
        --validate) validate=1;;
        --cluster-only) cluster_only=1;;
        -p|--prefix) shift
            prefix="$1";;
        -s|--subscription) shift
            subscription="$1";;
        -n|--servicename) shift
            servicename="$1";;
        -e|--env) shift
            env="$1";;
        -i|--instance) shift
            instance="$1";;
        -l|--location) shift
            location="$1";;
        --) if [[ ${discard_opts_after_doubledash} -eq 1 ]]; then break; fi;;
        *) extra_args=("${extra_args[@]}" "$1");;
    esac
    shift # next argument
done

# Now delete the ${dummy_arg} from ${extra_args[@]} array # http://stackoverflow.com/a/16861932/1219634
extra_args=("${extra_args[@]/${dummy_arg}}")

# Emit verbose output for parameters
echo_verbose "Parameters: "
echo_verbose "  help: ${help}"
echo_verbose "  debug: ${debug}"
echo_verbose "  verbose: ${verbose}"
echo_verbose "  validate: ${validate}"
echo_verbose "  cluster_only: ${cluster_only}"
echo_verbose "  prefix: $prefix"
echo_verbose "  servicename: $servicename"
echo_verbose "  env: $env"
echo_verbose "  instance: $instance"

if [ $help != 0 ] ; then
    echo "usage $(basename $0) [options]"
    echo " -s, --subscription  the subscription name or id [optional]"
    echo " -p, --prefix        the service name prefix, for example, 'vssvc'"
    echo " -n, --servicename   the service base name"
    echo " -e, --env           the service environment, 'dev', 'ppe', 'prod'"
    echo " -i, --instance      the service instance name"
    echo " -l, --location      the service primary location"
    echo "     --cluster-only  deploy only the regional cluster, not the environment or instance"
    echo "     --validate      validate deployment templates, do not deploy"
    echo " -d, --debug         emit debug info"
    echo " -v, --verbose       emit verbose info"
    echo " -h, --help          show help"
fi

if [[ -z $prefix ]] ; then
    echo_error "Parameter prefix is required" && exit 1
fi
if [[ -z $servicename ]] ; then
    echo_error "Parameter servicename is required" && exit 1
fi
if [[ -z $env ]] ; then
    echo_error "Parameter env is required" && exit 1
fi
if [[ -z $instance ]] ; then
    echo_error "Parameter instance is required" && exit 1
fi
if [[ -z $location ]] ; then
    echo_error "Parameter location is required" && exit 1
fi

function create_resource_group()
{
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
    create_resource_group $environment_rg $location
}

function create_instance_resource_group()
{
    create_resource_group $instance_rg $location
}

function deploy_cluster_environment_resources()
{
    create_environment_resource_group
    echo "$deployment_action cluster environment"
    local templatefile="$template_dir/cluster-environment.json"
    local keyvaultaccesspolicies_param="$param_dir/keyvaultAccessPolicies.$instance.json"
    echo_verbose "keyvaultaccesspolicies_param: $keyvaultaccesspolicies_param"
    az group deployment $deployment_verb -g "$environment_rg" \
        --template-file "$templatefile" \
        --parameters @$env_param \
        --parameters @$servicename_param \
        --parameters @$keyvaultaccesspolicies_param \
        -o jsonc
}

function deploy_cluster_instance_resources()
{
    create_instance_resource_group
    echo "$deployment_action cluster instance"
    local templatefile="$template_dir/cluster-instance.json"
    az group deployment $deployment_verb -g "$instance_rg" \
        --template-file "$templatefile" \
        --parameters @$env_param \
        --parameters @$servicename_param \
        --parameters @$instance_param \
        -o jsonc
}

function deploy_cluster_resources()
{
    local location="${1}"
    local templatefile="$template_dir/cluster-region.json"
    local clusterserviceprincipal_param="$param_dir/clusterServicePrincipal.$servicename.json"
    create_instance_resource_group
    echo "$deployment_action cluster in $instance_rg"
    echo_verbose "clusterserviceprincipal_param: $clusterserviceprincipal_param"
    az group deployment $deployment_verb -g "$instance_rg" \
        --template-file "$templatefile" \
        --parameters @$env_param \
        --parameters @$servicename_param \
        --parameters @$instance_param \
        --parameters @$clusterserviceprincipal_param \
        -o jsonc
}

set_keyvault_secret()
{
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

create_cluster_sp()
{
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

assign_cluster_sp_roles()
{
    local sp=$(az ad sp show --id "http://$cluster_sp_name" -o json) || return $?
    local appid=$(echo $sp | jq .appId  | tr -d '"')
    local scope="/subscriptions/$subscription/resourceGroups/$instance_rg"
    local role="Contributor"
    local az_command="az role assignment create --assignee $appid --scope $scope --role $role"
    echo_verbose "$az_command"
    $az_command
}

cluster_info()
{
    kubectl cluster-info
}

connect_to_aks()
{
    local cluster_name="${1}"
    local cluster_rg="${2}"
    az aks get-credentials --resource-group $cluster_rg --name $cluster_name
}

set_az_subscription()
{
    if [[ ! -z $subscription ]] ; then
        az account set -s $subscription >/dev/null || return $?
    fi
    local name=$(az account show --query name -o tsv) || return $?
    subscription=$(az account show --query id -o tsv) || return $?
    
    echo_verbose "Subscription: $name [$subscription]"
}

kubectl_apply()
{
    local file="${1}"
    local kubectl_command="kubectl apply -f $file"
    echo_verbose "$kubectl_command"
    $kubectl_command
}

# Script Variables
dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
template_dir="$( cd "$dir/../arm" >/dev/null 2>&1 && pwd )"
param_dir="$( cd "$dir/../arm/parameters" >/dev/null 2>&1 && pwd )"
k8s_dir="$( cd "$dir/../k8s" >/dev/null 2>&1 && pwd )"
charts_dir="$( cd "$dir/../charts" >/dev/null 2>&1 && pwd )"
environment_name="$prefix-$servicename-$env"
instance_name="$prefix-$servicename-$env-$instance"
environment_rg="$environment_name"
instance_rg="$instance_name"
keyvault_rg="$environment_rg"
keyvault_name="$environment_name-kv"
cluster_sp_name="$environment_name-cluster-sp"
cluster_name="$instance_name-cluster"
cluster_rg="$instance_rg"

# Parameters files
servicename_param="$param_dir/serviceName.$servicename.json"
env_param="$param_dir/env.$env.json"
instance_param="$param_dir/instance.$instance.json"

# Initialize the verb to use for deployment
deployment_action="Creating"
deployment_verb="create"
if [ $validate != 0 ] ; then
    deployment_action="Validating"
    deployment_verb="validate"
fi

echo_verbose "Script variables:"
echo_verbose "  charts_dir: $charts_dir"
echo_verbose "  deployment_action: $deployment_action"
echo_verbose "  deployment_verb: $deployment_verb"
echo_verbose "  env_param: $env_param"
echo_verbose "  environment_name: $environment_name"
echo_verbose "  environment_rg: $environment_rg"
echo_verbose "  instance_name: $instance_name"
echo_verbose "  instance_rg: $instance_rg"
echo_verbose "  k8s_dir: $k8s_dir"
echo_verbose "  keyvault_name: $keyvault_name"
echo_verbose "  keyvault_rg: $keyvault_rg"
echo_verbose "  param_dir: $param_dir"
echo_verbose "  servicename_param: $servicename_param"
echo_verbose "  tempaltedir: $template_dir"

function main()
{
    set_az_subscription || return $?

    # Deploy base resources
    if [ $cluster_only == 0 ] ; then
        deploy_cluster_environment_resources || return $?
        deploy_cluster_instance_resources || return $?
    fi

    # Create the cluster service principal and store in key vault
    create_cluster_sp || return $?
    assign_cluster_sp_roles || return $?

    # Deploy the cluster to a single region
    location="eastus"
    deploy_cluster_resources $location || return $?

    # Connect to the cluster
    cluster_rg="$instance_rg"
    cluster_name=$"$instance_name-${location}-cluster"
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
