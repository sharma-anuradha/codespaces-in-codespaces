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

add_option "stamp-location:"
cluster_location=""

add_option "regions:" "r:"
regions=

add_option "validate-only"
validate_only=0

add_option "team-group-id:"
team_group_id=

add_option "ssl-cert-kv-name:"
ssl_cert_kv_name=

add_option "ssl-cert-secrets:"
ssl_cert_secrets=

add_option "dns-name:"
dns_name=

add_option "cluster-version:"
cluster_version=

add_option "cluster-node-count:"
cluster_node_count=

add_option "signlr-enabled:"
signlr_enabled="true"

add_option "port-forwarding-enabled:"
port_forwarding_enabled="false"

add_option "signlr-capacity:"
signlr_capacity=

add_option "signlr-capacity-sec:"
signlr_capacity_sec=

add_option "storage-account-prefix:"
storage_account_prefix=

add_option "port-forwarding-service-name:"
port_forwarding_service_name=

add_option "stage:"
stage_bootstrap="bootstrap"
stage_common="common"
stage_stamp="stamp"
stage_cluster="cluster"
stage=

function isStage() {
    [ "$stage" == "$1" ] && return 0 && return 1
}

function stageBootstrap() {
    isStage $stage_bootstrap
}

function stageCommon() {
    isStage $stage_common
}

function stageStamp() {
    isStage $stage_stamp
}

function stageCluster() {
    isStage $stage_cluster
}

function validStage() {
    stageBootstrap || stageCommon || stageStamp || stageCluster || return 1
    return 0
}

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
        --stage) shift
            stage="$1";;
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
        --stamp-location) shift
            cluster_location="$1";;
        -l|--location) shift
            location="$1";;
        -r|--regions) shift
            regions="$1";;
        --team-group-id) shift
            team_group_id="$1";;
        --ssl-cert-kv-name) shift
            ssl_cert_kv_name="$1";;
        --ssl-cert-secrets) shift
            ssl_cert_secrets="$1";;
        --dns-name) shift
            dns_name="$1";;
        --cluster-version) shift
            cluster_version="$1";;
        --cluster-node-count) shift
            cluster_node_count="$1";;
        --storage-account-prefix) shift
            storage_account_prefix="$1";;
        --port-forwarding-service-name) shift
            port_forwarding_service_name="$1";;
        --signlr-enabled) shift
            signlr_enabled="$1";;
        --signlr-capacity) shift
            signlr_capacity="$1";;
        --signlr-capacity-sec) shift
            signlr_capacity_sec="$1";;
        --port-forwarding-enabled) shift
            port_forwarding_enabled="$1";;
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
echo_debug "  stage: ${stage}"
echo_debug "  subscription: ${subscription}"
echo_debug "  prefix: ${prefix}"
echo_debug "  name: ${name}"
echo_debug "  env: ${env}"
echo_debug "  instance: ${instance}"
echo_debug "  stamp: ${stamp}"
echo_debug "  cluster_location: ${cluster_location}"
echo_debug "  cluster_version: ${cluster_version}"
echo_debug "  cluster_node_count: ${cluster_node_count}"
echo_debug "  location: ${location}"
echo_debug "  regions: ${regions}"
echo_debug "  team_group_id: ${team_group_id}"
echo_debug "  ssl_cert_kv_name: ${ssl_cert_kv_name}"
echo_debug "  ssl_cert_secrets: ${ssl_cert_secrets}"
echo_debug "  storage_account_prefix: ${storage_account_prefix}"
echo_debug "  port_forwarding_service_name: ${port_forwarding_service_name}"
echo_debug "  signlr-capacity: ${signlr-capacity}"
echo_debug "  signlr_capacity_sec: ${signlr_capacity_sec}"
echo_debug "  validate_only: ${validate_only}"
echo_debug "  help: ${help}"
echo_debug "  dry_run: ${utilities_dry_run}"
echo_debug "  verbose: ${utilities_verbose}"
echo_debug "  debug: ${utilities_debug}"

if [ $help != 0 ] ; then
    echo "usage $(basename $0) [options]"
    echo "     --stage                            $stage_bootstrap, $stage_common, $stage_stamp, $stage_cluster"
    echo " -s, --subscription                     the subscription name or id [optional]"
    echo " -p, --prefix                           the service name prefix, for example, 'vsclk'"
    echo " -n, --name                             the service base name"
    echo " -e, --env                              the service environment, 'dev', 'ppe', 'prod', 'rel'"
    echo " -i, --instance                         the service instance name, defaults to env name"
    echo " -l, --location                         the service primary location"
    echo " -m, --stamp                            the service stamp name"
    echo "     --stamp-location                   the service stamp location"
    echo " -r, --regions                          the locations of all service stamps (for common/global resources)"
    echo "     --dns-name                         the service instance dns name"
    echo "     --cluster-version                  the Kubernetes cluster version to use"
    echo "     --signlr-enbabled                  indicates whether to deploy signlr resource (default is true)"
    echo "     --signlr-capacity                  the capacity of the primary signalR resource"
    echo "     --signlr-capacity-sec              the capacity of the secondary signalR resource"
    echo "     --team-group-id                    the dev team group object id"
    echo "     --ssl-cert-kv-name                 the azure key vault that contains the default ssl certificate"
    echo "     --ssl-cert-secrets                 the list of key-value pairs for all ssl certificates in for form 'deployed-name:keyvault-name,'"
    echo "     --storage-account-prefix           the storage account prefix (default is vso)"
    echo "     --validate-only                    validate ARM templates, do not deploy"
    echo "     --dry-run                          do not create or deploye Azure resources"
    echo " -h, --help                             show help"
    echo " -v, --verbose                          emit verbose info"
    echo " -d, --debug                            emit debug info"
    exit 0
fi

if [[ -z $stage ]]; then
    echo_error "Parameter --stage is required" && exit 1
fi

if ! validStage; then
    echo_error "Parameter --stage '$stage' is not valid. Valid values are $stage_bootstrap, $stage_common, $stage_stamp, $stage_cluster" && exit 1
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
    echo_error "Parameter --instance is required" && exit 1
fi
if [[ -z $stamp ]] ; then
    echo_error "Parameter --stamp is required" && exit 1
fi
if [[ -z $location ]] ; then
    echo_error "Parameter --location is required" && exit 1
fi
if [[ -z $cluster_location ]] ; then
    cluster_location="${location}"
    echo_warning "Parameter --stamp-location is not specified, using '${cluster_location}'"
fi
if [[ -z $regions ]] ; then
    if stageCommon; then
        echo_error "Parameter --regions is required" && exit 1
    fi
fi
if [[ -z $team_group_id ]] ; then
    echo_error "Parameter --team-group-id is required" && exit 1
fi
if [[ -z $ssl_cert_kv_name ]] ; then
    echo_error "Parameter --ssl-cert-kv-name is required" && exit 1
fi
if [[ -z $ssl_cert_secrets ]] ; then
    if stageCluster; then
        echo_error "Parameter --ssl-cert-secrets is required" && exit 1
    fi
fi
if [[ -z $dns_name ]] ; then
    if stageCommon || stageStamp; then
        echo_error "Parameter --dns-name is required" && exit 1
    fi
fi
if [[ -z ${cluster_version} ]] ; then
    echo_error "Parameter --cluster-version is required" && exit 1
fi
if [[ -z ${cluster_node_count} ]] ; then
    echo_error "Parameter --cluster-node-count is required" && exit 1
fi
if [[ -z ${signlr_capacity} ]] ; then
    if [[ "${signlr_enabled}" = "true" ]] ; then
        echo_error "Parameter --signlr-capacity is required" && exit 1
    fi
fi
if [[ -z ${signlr_capacity_sec} ]] ; then
    if [[ "${signlr_enabled}" = "true" ]] ; then
        echo_error "Parameter --signlr-capacity-sec is required" && exit 1
    fi
fi
if [[ -z $storage_account_prefix ]] ; then
    storage_account_prefix="vso"
    echo_warning "Parameter --storage-account-prefix is not specified, using '${storage_account_prefix}'"
fi
if [[ -z $port_forwarding_service_name ]] ; then
    port_forwarding_service_name="pf"
    echo_warning "Parameter --port-forwarding-service-name is not specified, using '${port_forwarding_service_name}'"
fi

function az_group_tag()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local resource_group="$1"
    local tagName="$2"
    local tagValue="$3"
    if [ ! -z "$tagName" ]; then
        if [ ! -z "$tagValue" ]; then
            exec_dry_run "az group update --name $resource_group --set tags.${tagName}=${tagValue} --query tags --output jsonc" || return $?
        fi
    fi
}

function az_group_create()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local resource_group="${1}"
    local location="${2}"
    echo_info "Creating resource group '$resource_group' in '$location'"
    local az_group_create_command="az group create --name $resource_group --location $location --query id --output tsv"
    exec_dry_run "${az_group_create_command}" || return $?

    # Tag the visual studio online environment name
    az_group_tag $resource_group "vsoEnvironment" "${environment_name}" || return $?
    az_group_tag $resource_group "vsoInstance" "${instance_name}" || return $?

    # Tag requested for
    local build_requestedfor="$(set +u; echo "${BUILD_REQUESTEDFOREMAIL}")"
    if [ ! -z "${build_requestedfor}" ]; then
        az_group_tag "$resource_group" "requestedFor" "${build_requestedfor}" || return $?
    fi
}

function create_environment_keyvault()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local resource_group=${keyvault_rg}
    local vault_name="${1}"
    local enabledForDeployment="true"
    local enabledForTemplateDeployment="true"

    # Create on existing overwrites access policies. Don't recreate if it alrady exists.
    if ! az_keyvault_exists $vault_name; then
        echo_info "Creating keyvault '${vault_name}'"
        local az_keyvault_create_command="az keyvault create --resource-group ${resource_group} --name ${vault_name} --location $location --enabled-for-deployment ${enabledForDeployment} --enabled-for-template-deployment ${enabledForTemplateDeployment} --query id --output tsv"
        exec_dry_run "${az_keyvault_create_command}" || return $?
    else
        echo_info "Keyvault '${vault_name} already exists.'"
    fi

    # Ensure that the current sp or user has access to the keyvault
    local user_type="$(exec_dry_run_with_stdout "dry-run-user-type" "az account show --query user.type -o tsv")"
    if [ "${user_type}" = "servicePrincipal" ]; then
        local sp_name="$(exec_dry_run_with_stdout "dry-run-sp-name" "az account show --query user.name -o tsv")"
        local sp_id="$(exec_dry_run_with_stdout "dry-run-sp-id" "az ad sp show --id $sp_name --query objectId -o tsv")"
        echo_info "Granting keyvault access to the currently logged in service principal '${sp_name}'"
        az_keyvault_set_policy "${vault_name}" "${sp_name}" "${sp_id}" "list get set" "list get" || return $?
    else
        local user_name="$(exec_dry_run_with_stdout "dry-run-user-name" "az account show --query user.name -o tsv")"
        local user_id="$(exec_dry_run_with_stdout "dry-run-user-id" "az ad user show --upn-or-object-id $user_name --query objectId -o tsv")"
        echo_info "Granting keyvault access to the currently logged in user '${user_name}'"
        az_keyvault_set_policy "${vault_name}" "${user_name}" "${user_id}" "list get set" "list get" || return $?
    fi
}

function az_keyvault_exists()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local keyvault_name="${1}"
    local az_command="az keyvault show --name $keyvault_name"
    get_dry_run && return 1 # doesn't exist for dry-run
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function create_environment_container_registry()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local resource_group=${registry_rg}
    local sku="Standard"
    local admin_enabled="false"

    echo_info "Creating container registry '${registry_name}'"
    local az_command="az acr create --resource-group ${registry_rg} --name ${registry_name} --location $location --sku ${sku} --admin-enabled ${admin_enabled} --query id --output tsv"
    exec_dry_run "${az_command}" || return $?
}

function az_group_deployment_create()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local arm_template_name="${1}"
    local resource_group="${2}"
    local additional_params="$(set +u; echo ${3})"
    local template_file="${template_dir}/${arm_template_name}.json"
    local parameters_file="$parameters_dir/${arm_template_name}.parameters.json"
    # use --output json so that error message don't get swallowed
    local az_command="az deployment group $deployment_verb --subscription ${subscription_id} --resource-group ${resource_group} --template-file ${template_file} --parameters @${parameters_file} ${additional_params} --output json"
    exec_dry_run "$az_command" || return $?
}

function region_shortcodes_to_azure_locations()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local azure_locations="${regions}"
    
    # Replace all of our service shortcodes with the real azure location names
    # TODO: Add more mappings here for each new region we onboard
    azure_locations=${azure_locations/usw2/westus2}
    azure_locations=${azure_locations/usec/eastus2euap}
    azure_locations=${azure_locations/use/eastus}
    azure_locations=${azure_locations/euw/westeurope}
    azure_locations=${azure_locations/asse/southeastasia}

    echo "${azure_locations}"
}

function ensure_service_principals()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    ensure_service_principal $keyvault_name "devops" || return $?
    ensure_service_principal $keyvault_name "app" || return $?
    ensure_service_principal $keyvault_name "cluster" || return $?

    if [[ "${signlr_enabled}" != "true" ]] ; then
        ensure_service_principal $sts_keyvault_name "sts" || return $?
    fi
}

function ensure_aes_private_keys()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"

    echo_verbose "Generating AES secrets."

    local aes_key=$(openssl rand -hex 16)
    local aes_iv=$(openssl rand -hex 16)

    echo_verbose "AES secrets generated."

    local keys_prefix="Config"
    local set_secret=az_keyvault_secret_set_if_unset
    ${set_secret} $keyvault_name "${keys_prefix}-AesKey" "$aes_key" || return $?
    ${set_secret} $keyvault_name "${keys_prefix}-AesIV" "$aes_iv" || return $?

    echo_verbose "AES secrets generated and set."
}

function get_logged_in_user_id()
{
    local name="$(exec_dry_run_with_stdout "dry-run-user" "az account show --query user.name -o tsv")"
    local type="$(exec_dry_run_with_stdout "dry-run-user-type" "az account show --query user.type -o tsv")"
    local objectId=
    if [ "$type" == "servicePrincipal" ]; then
        echo_error "The stage '${stage}' cannot be run under a service principal due to required AAD applicaiton permssions."
        return 1
    else
        objectId="$(exec_dry_run_with_stdout "dry-run-user-object-id" "az ad user show --upn-or-object-id $name --query objectId --output tsv")" || return $?
    fi

    echo "$objectId"
}

function init_cluster_envrionment_base_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    # Before creating anything, be sure the current user has Owner access to assign RBAC
    echo_info "Checking that the logged in use has Owner access."
    local logged_in_object_id="$(get_logged_in_user_id)" || return $?
    local dry_run_assignments="Owner" # "Owner" or "" to test failure
    local az_role_assignment_list_cmd="az role assignment list --assignee $logged_in_object_id --scope /subscriptions/$subscription_id --role Owner --output json"
    echo_verbose "${az_role_assignment_list_cmd}"
    local assignments="$(exec_dry_run_with_stdout "${dry_run_assignments}" "${az_role_assignment_list_cmd}")"
    if [ -z "${assignments}" ]; then
        local name="$(exec_dry_run_with_stdout "dry-run-user" "az account show --query user.name -o tsv")"
        echo_error "User or service principal '$name' does not have Owner role in subscription $subscription_name."
        echo_error "Unable to create base resources and assign RBAC permissions."
        return 1
    fi

    # We always need the environment resource group and key vault before running ARM templates,
    # so we have somewhere to pull parameter secrets from.
    # We also need the other resource groups in order to assign RBAC to service principals
    # that get created and added to the keyvault
    
    az_group_create $environment_rg $location || return $?
    az_group_create $instance_rg $location || return $?
    az_group_create $cluster_rg $cluster_location || return $?

    # Creating region specific Windows image resource group and gallery
    create_region_windows_gallery || return $?

    # Create the keyvault to store service principal attributes
    create_environment_keyvault ${keyvault_name} || return $?
    create_environment_keyvault ${billing_keyvault_name} || return $?

    if [[ "${signlr_enabled}" != "true" ]] ; then
        create_environment_keyvault ${sts_keyvault_name} || return $?
    fi

    # Create the container registry during bootstrapping so we can assign the acrpull role
    create_environment_container_registry || return $?

    # We need these service principals to exist and to be used by
    # deployment, or to be referenced by ARM parameters.
    ensure_service_principals

    # RBAC for devops-sp
    az_role_assignment_create_scope_resource_group "devops" ${environment_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "devops" ${instance_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "devops" ${cluster_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "devops" ${instance_images_rg} "Contributor" || return $?
    az_keyvault_set_policy_service_principal_list_get "devops" ${keyvault_name} || return $?
    az_keyvault_set_policy_service_principal_list_get "devops" ${billing_keyvault_name} || return $?
    az_keyvault_set_policy_service_principal_list_get "devops" ${ssl_cert_kv_name} || return $?
    

    # RBAC for app-sp
    az_role_assignment_create_scope_resource_group "app" ${environment_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "app" ${instance_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "app" ${cluster_rg} "Contributor" || return $?
    az_role_assignment_create_scope_resource_group "app" ${instance_images_rg} "Contributor" || return $?
    # RBAC for RG used by ComputeProvisiong service
    az_role_assignment_create_scope_acrpull_for_computeService || return $?

    az_keyvault_set_policy_service_principal_list_get "app" ${keyvault_name} || return $?
    az_keyvault_set_policy_service_principal_list_get "app" ${billing_keyvault_name} || return $?
    az_keyvault_set_policy_service_principal_list_get "app" ${ssl_cert_kv_name} || return $?

    if [[ "${signlr_enabled}" != "true" ]] ; then
        # RBAC for sts-sp
        az_role_assignment_create_scope_resource_group "sts" ${environment_rg} "Contributor" || return $?
        az_keyvault_set_policy_service_principal_list_get "sts" ${sts_keyvault_name} || return $?
    fi

    # RBAC for cluster-sp
    az_role_assignment_create_scope_resource_group "cluster" ${cluster_rg} "Contributor" || return $?
    az_role_assignment_create_scope_acrpull "cluster" ${registry_rg} ${registry_name} || return $?
    az_keyvault_set_policy_service_principal_list_get "cluster" ${keyvault_name} || return $?
    az_keyvault_set_policy_service_principal_list_get "cluster" ${ssl_cert_kv_name} || return $?

    # RBAC for team-contributors
    if [ "${env}" == "dev" ]; then
        # All
        local secret_permissions="backup delete get list purge recover restore set"
        local certificate_permissions="backup create delete deleteissuers get getissuers import list listissuers managecontacts manageissuers purge recover restore setissuers update"
    else
        local secret_permissions="list"
        local certificate_permissions="list"
    fi
    az_keyvault_set_policy "${keyvault_name}" "team-contributors-group" "${team_group_id}" "${secret_permissions}" "${certificate_permissions}" || return $?

    # Other RBAC
    local azureServiceDeploymentInternalId="175890fa-bd80-4743-86d9-faed7653f078"
    az_keyvault_set_policy ${keyvault_name} "AzureServiceDeploymentInternal" ${azureServiceDeploymentInternalId} "list get" "list get" || return $?
}

function create_region_windows_gallery()
{
    # Creating Windows image resource group. This region specific resource group will contain
    # the image gallery and image definition for the Windows images 
    echo_info "Creating resource group '$instance_images_rg' in '$stamp'"
    local region_gallery="gallery_${stamp}"
    az_group_create $instance_images_rg $cluster_location || return $?
    
    # Image Gallery
    echo_info "Creating image gallery in resoruce group '$instance_images_rg' named '$region_gallery'"
    local az_command="az sig create --resource-group ${instance_images_rg} --gallery-name ${region_gallery}"
    exec_dry_run ${az_command} || return $?

    # Image Definition
    echo_info "Creating Windows image definition named windows, in image gallery: '$region_gallery'"
    local params="--offer VSOnline --os-type Windows --publisher Microsoft --sku VisualStudio"
    local az_command1="az sig image-definition create -i windows --resource-group ${instance_images_rg} --gallery-name ${region_gallery} ${params}"
    exec_dry_run ${az_command1} || return $?
}

function deploy_cluster_environment_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Deploying cluster environment ARM resources (${environment_rg})"
    az_group_deployment_create "cluster-environment" "${environment_rg}" || return $?
}

function deploy_cluster_instance_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Deploying cluster instance ARM resources (${instance_rg})"
    local profile_name="${instance_tm}"
    local pf_profile_name="${pf_instance_tm}"
    local resource_group="${instance_rg}"
    az_group_deployment_create "cluster-instance" "${resource_group}" || return $?

    # signalR service specific instance
    if [[ "${signlr_enabled}" = "true" ]] ; then
        az_group_deployment_create "cluster-instance-signalr" "${resource_group}" || return $?
    fi

    if ! az_trafficmanager_exists $resource_group $profile_name ; then
        az_group_deployment_create "cluster-instance-tm" "${resource_group}" || return $?
    else
        echo_verbose "Traffic manager ${profile_name} already exists; skipping creation."
    fi

    if [[ "${port_forwarding_enabled}" = "true" ]] ; then
        if ! az_trafficmanager_exists $resource_group $pf_profile_name ; then
            az_group_deployment_create "cluster-instance-tm" "${resource_group}" " --parameters serviceName=${port_forwarding_service_name}" || return $?
        else
            echo_verbose "Traffic manager ${pf_profile_name} already exists; skipping creation."
        fi
    fi
}

function deploy_cluster_stamp_resources()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Deploying cluster stamp ARM resources (${cluster_rg})"

    az_group_deployment_create "cluster-stamp" "${cluster_rg}" || return $?

    if [[ "${port_forwarding_enabled}" = "true" ]] ; then
        az_group_deployment_create "cluster-stamp-port-forwarding" "${cluster_rg}" " --parameters serviceName=${port_forwarding_service_name}" || return $?

        az_group_deployment_create "cluster-stamp-service-bus" "${cluster_rg}" || return $?
    fi


    if [[ "${signlr_enabled}" = "true" ]] ; then
        az_group_deployment_create "cluster-stamp-signalr" "${cluster_rg}" || return $?

        # We need the signalR secrets on the keyvault to be found by our services
        az_keyvault_signalr_secret_set ""
        az_keyvault_signalr_secret_set "-primary-1"
        az_keyvault_signalr_secret_set "-secondary"

        # Note: when signalR is enabled we don't want to deploy any addtional resource
        return
    fi

    # deploy the data-plane-specific storage accounts for all data-plane regions
    local storage_account_infixes="cq si vm bl"
    local storage_account_region_codes=$(get_dataplane_region_codes)
    echo_info "Deploying cluster stamp data-plane storage accounts for ${storage_account_region_codes} (${cluster_rg})"
    for infix in $storage_account_infixes; do
        for region_code in $storage_account_region_codes; do
            local storage_account_name="${storage_account_prefix}${env}${instance}${stamp}${infix}${region_code}"
            if ! az_storage_exists $storage_account_name; then
                echo_info "Deploying cluster stamp data-plane storage account (${storage_account_name})"
                local params=""
                params="${params} --parameters storageAccountPrefix=${storage_account_prefix}"
                params="${params} --parameters storageAccountInfix=${infix}"
                params="${params} --parameters storageAccountRegionCode=${region_code}"
                az_group_deployment_create "cluster-stamp-storage" "${cluster_rg}" "${params}" || return $?
            else
                echo_verbose "Storage account ${storage_account_name} already exists; skipping creation."
            fi
        done
    done

    # Configure the billing storage accounts and SAS tokens
    local storage_account_region_codes=$(get_dataplane_region_codes)
    local infix="bl"
    for region_code in $storage_account_region_codes; do
        local storage_account_name="${storage_account_prefix}${env}${instance}${stamp}${infix}${region_code}"
        echo_info "Preparing cluster stamp data-plane billing storage account (${storage_account_name})"

        # Create error reporting table
        local table_name="ErrorReportingTable"
        if ! az_storage_table_exists ${storage_account_name} ${table_name}; then
            echo_info "Creating table ${table_name} in ${storage_account_name}"
            local az_command="az storage table create --account-name ${storage_account_name} --name ${table_name}"
            exec_dry_run ${az_command} || return $?
        fi

        # Create usage reporting table
        local table_name="UsageReportingTable"
        if ! az_storage_table_exists ${storage_account_name} ${table_name}; then
            echo_info "Creating table ${table_name} in ${storage_account_name}"
            local az_command="az storage table create --account-name ${storage_account_name} --name ${table_name}"
            exec_dry_run ${az_command} || return $?
        fi

        # Create error reporting queue
        local queue_name="error-reporting-queue"
        if ! az_storage_queue_exists ${storage_account_name} ${queue_name}; then
            echo_info "Creating queue ${queue_name} in ${storage_account_name}"
            local az_command="az storage queue create --account-name ${storage_account_name} --name ${queue_name}"
            exec_dry_run ${az_command} || return $?
        fi

        # Create usage reporting queue
        local queue_name="usage-reporting-queue"
        if ! az_storage_queue_exists ${storage_account_name} ${queue_name}; then
            echo_info "Creating queue ${queue_name} in ${storage_account_name}"
            local az_command="az storage queue create --account-name ${storage_account_name} --name ${queue_name}"
            exec_dry_run ${az_command} || return $?
        fi

        # Genmerate and store SAS tokens in billing keyvault
        local permissions="raup"
        local resource_types="sco"
        local services="qt"
        local expiry="$(date -u -d "+1 year" -I)T00:00:00Z"
        local az_command="az storage account generate-sas --account-name ${storage_account_name} --expiry ${expiry} --permissions ${permissions} --resource-types ${resource_types} --services ${services} --https-only --output tsv"
        local sas_token="$(exec_dry_run_with_stdout "dry-run-generate-sas" "${az_command}")" || return $?
        echo_info "Updating sas token secret name ${storage_account_name} in ${billing_keyvault_name}, expires ${expiry}"
        az_keyvault_secret_set ${billing_keyvault_name} "${storage_account_name}" "${sas_token}" "${expiry}" || return $?
    done

    # deploy the top level powershell script for windows VM initialization
    local storage_account_region_codes=$(get_dataplane_region_codes)
    local infix="vm"
    local windowsInitShimContainer="windows-init-shim"
    for region_code in $storage_account_region_codes; do
        local storage_account_name="${storage_account_prefix}${env}${instance}${stamp}${infix}${region_code}"
        echo_info "Deploying storage container for ${windowsInitShimContainer} in ${storage_account_name}"
        if ! az_container_exists $storage_account_name $windowsInitShimContainer; then
            echo_info "Deploying container ${windowsInitShimContainer}"
            az_container_create $storage_account_name $windowsInitShimContainer
        fi

        echo_info "Deploying the latest version of WindowsInitShim.ps1"
        az_upload_file_to_container "${storage_account_name}" "${windowsInitShimContainer}" "${powershell_scripts_dir}/WindowsInitShim.ps1" "WindowsInitShim.ps1"
    done

    # deploy the data-plane-specific batch account for all data-plane regions
    local batch_account_prefix="${storage_account_prefix}"
    local batch_account_region_codes=$(get_dataplane_region_codes)
    local values_file="${charts_dir}/valuefiles/values.${env}.yaml"
    # Separate out var declarations due to http://mywiki.wooledge.org/BashPitfalls#local_var.3D.24.28cmd.29
    local gcs_environment
    local gcs_account
    local gcs_namespace
    local gcs_role
    local gcs_tenant
    local gcs_pfx_base64
    gcs_environment=$(yml_get_value ${values_file} 'AzSecPack_GCS_Environment') || return $?
    gcs_account=$(yml_get_value ${values_file} 'AzSecPack_GCS_Account') || return $?
    gcs_namespace=$(yml_get_value ${values_file} 'AzSecPack_Namespace') || return $?
    gcs_role=$(yml_get_value ${values_file} 'AzSecPack_Role') || return $?
    gcs_tenant=$(yml_get_value ${values_file} 'AzSecPack_Tenant') || return $?
    gcs_pfx_base64=$(az keyvault secret show --vault-name "${prefix}-core-${env}-kv" --name "${prefix}-core-${env}-monitoring" --query value --output tsv) || return $?
    echo_info "Deploying cluster stamp data-plane batch accounts for ${batch_account_region_codes} (${cluster_rg})"
    for region_code in $batch_account_region_codes; do
        local infix="ba"
        local batch_account_name="${batch_account_prefix}${env}${instance}${stamp}${infix}${region_code}"
        # if ! az_batch_exists $batch_account_name $cluster_rg; then
        echo_info "Deploying cluster stamp data-plane batch account (${batch_account_name})"
        local start_task_tempfile=$(mktemp)
        cat "${azurebatch_scripts_dir}/start-commandline.sh" >| ${start_task_tempfile}
        sed -i -e "s/__REPLACE_GCS_ENVIRONMENT__/${gcs_environment}/g" ${start_task_tempfile}
        sed -i -e "s/__REPLACE_GCS_ACCOUNT__/${gcs_account}/g" ${start_task_tempfile}
        sed -i -e "s/__REPLACE_GCS_NAMESPACE__/${gcs_namespace}/g" ${start_task_tempfile}
        sed -i -e "s/__REPLACE_GCS_ROLE__/${gcs_role}/g" ${start_task_tempfile}
        sed -i -e "s/__REPLACE_GCS_TENANT__/${gcs_tenant}/g" ${start_task_tempfile}
        sed -i -e "s;__REPLACE_GCS_PFX_BASE64__;${gcs_pfx_base64};g" ${start_task_tempfile}
        local batch_start_task_command_line_base64=$(cat ${start_task_tempfile} | base64 --wrap=0)
        rm ${start_task_tempfile}
        local params=""
        params="${params} --parameters accountNamePrefix=${batch_account_prefix}"
        params="${params} --parameters accountNameRegionCode=${region_code}"
        params="${params} --parameters startTaskCommandLineBase64=${batch_start_task_command_line_base64}"
        az_group_deployment_create "cluster-stamp-batch" "${cluster_rg}" "${params}" || return $?
        # else
            # echo_verbose "Batch account ${batch_account_name} already exists; skipping creation."
        # fi
    done
}

function az_keyvault_signalr_secret_set()
{
    echo_info "Updating SingalR connection string secret:${1}"
    local resource_group="${cluster_rg}"
    local signalr_name="${cluster_rg}-signalr${1}"
    local get_signalr_connection_string_cmd="az signalr key list --name ${signalr_name} --resource-group ${resource_group} --query primaryConnectionString -o tsv"
    echo_verbose "${get_signalr_connection_string_cmd}"
    if get_dry_run; then
        local primaryKey="dry-run-primary-key"
    else
        local primaryKey="$(${get_signalr_connection_string_cmd})" || return $?
    fi
    local secret_name="Config-SignalRConnectionString-${stamp}${1}"
    az_keyvault_secret_set $keyvault_name $secret_name $primaryKey
}

# TODO: Ideally these are passed in as parameters from the yaml pipeline.
# But for now, they aren't changing and are simply inlined.
function get_dataplane_region_codes()
{
    # For now, each stamp has one data-plane in its own region
    echo "${stamp}"
}

function az_storage_table_exists()
{
    local storage_account_name="${1}"
    local table_name="${2}"
    local az_command="az storage table exists --account-name ${storage_account_name} --name ${table_name}"

    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_storage_queue_exists()
{
    local storage_account_name="${1}"
    local table_name="${2}"
    local az_command="az storage queue exists --account-name ${storage_account_name} --name ${table_name}"

    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_container_exists()
{
    local accountName="${1}"
    local containerName="${2}"
    local az_command="az storage container show --name ${containerName} --account-name ${accountName}"
    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_container_create()
{
    local accountName="${1}"
    local containerName="${2}"
    local az_command="az storage container create --name ${containerName} --account-name ${accountName}"
    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_upload_file_to_container()
{
    local accountName="${1}"
    local containerName="${2}"
    local fileOnDisk="${3}"
    local fileOnBlob="${4}"
    local az_command="az storage blob upload --file ${fileOnDisk} --container-name ${containerName} --account-name ${accountName} --name ${fileOnBlob}"
    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_storage_exists()
{
    local name="${1}"
    local az_command="az storage account show --name ${name}"
    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_batch_exists()
{
    local name="${1}"
    local resource_group="${2}"
    local az_command="az batch account show --name ${name}  --resource-group ${resource_group}"
    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_keyvault_secret_set()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local vault_name="${1}"
    local name="${2}"
    local value="${3}"
    local expires="$(set +u; echo ${4})"

    local az_command="az keyvault secret set --vault-name $vault_name --name $name --value $value --query id --output tsv"
    local az_command_redacted="az keyvault secret set --vault-name $vault_name --name $name --value *** --query id --output tsv"

    if [ ! -z $expires ] ; then
        az_command="${az_command} --expires ${expires}"
        az_command_redacted="${az_command_redacted} --expires ${expires}"
    fi

    if get_dry_run; then
        exec_dry_run "${az_command_redacted}"
    else
        echo_verbose "${az_command_redacted}"
        $az_command
    fi
}

function az_keyvault_secret_get()
{
    local vault_name="${1}"
    local name="${2}"

    local az_command="az keyvault secret show --vault-name $vault_name --name $name --query value --output tsv"
    $az_command
}

function az_keyvault_secret_exists()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local vault_name="${1}"
    local name="${2}"
    local vault_name="$(set +u; echo ${2})"

    local az_command="az keyvault secret show --vault-name $vault_name --name $name"
    get_dry_run && return 1 # doesn't exist for dry-run
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_keyvault_secret_set_if_unset()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local vault_name="${1}"
    local name="${2}"
    local value="${3}"
    local expires="$(set +u; echo "${4}")"

    if ! az_keyvault_secret_exists $vault_name $name; then
        az_keyvault_secret_set $vault_name $name $value $expires
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

function ensure_service_principal() {
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local vault_name="${1}"
    local base_name="${2}"
    local sp_name="$(get_sp_name ${base_name})" || return $?
    if stageBootstrap; then
        local user_type="$(exec_dry_run_with_stdout "dry-run-user-type" "az account show --query user.type -o tsv")"
        if [ "${user_type}" = "servicePrincipal" ]; then
            echo_info "Skipping bootstrapping service principal '${sp_name}' while running as service principal"
            read_service_principal_secrets $vault_name $base_name
        else
            bootstrap_service_principal $vault_name $base_name
        fi
    else
        read_service_principal_secrets $vault_name $base_name
    fi
}

function read_service_principal_secrets() {
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local vault_name="${1}"
    local base_name="${2}"
    local secret_base_name="${base_name}-sp"
    local sp_name="$(get_sp_name ${base_name})" || return $?

    echo_info "Reading keyvault properties for service principal '${sp_name}'"
    if get_dry_run; then
        local spid="dry-run-${secret_base_name}-id"
        local appid="dry-run-${secret_base_name}-appid)"
    else
        local spid="$(az_keyvault_secret_get $vault_name ${secret_base_name}-id )" || return $?
        local appid="$(az_keyvault_secret_get $vault_name ${secret_base_name}-appid )" || return $?
    fi

    # track the object ids for later
    echo_verbose "Setting principal_objectids and principal_appids"
    principal_objectids[${sp_name}]=$spid
    echo_debug "Service principal id ${sp_name} : ${principal_objectids[$sp_name]}"
    principal_appids[${sp_name}]=$appid
    echo_debug "Service principal appid ${sp_name} : ${principal_appids[$sp_name]}"
}

function bootstrap_service_principal()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local vault_name="${1}"
    local base_name="${2}"
    local secret_base_name="${base_name}-sp"
    local sp_name="$(get_sp_name ${base_name})" || return $?
    local sp=

    if ! stageBootstrap; then
        echo_error "Function ${FUNCNAME[0]} requires stage ${stage_bootstrap}"
    fi

    if ! $(az_ad_sp_exists $sp_name); then
        echo_info "Creating service principal '${sp_name}'"
        local passwordLength=128
        local password=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w ${passwordLength} | head -n 1)
        password="${password}!"
        local expires="$(date -u -d "+1 year" -I)T00:00:00Z"
        az_ad_sp_create_for_rbac $sp_name $password || return $?
        local set_secret=az_keyvault_secret_set
    else
        echo_verbose "Service principal '${sp_name}' already exists."
        local password=
        local expires=
        local set_secret=az_keyvault_secret_set_if_unset
    fi

    echo_info "Getting service principal '${sp_name}' attributes"
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
    echo_info "Updating properties for service principal '${sp_name}' in keyvault '${vault_name}'"
    ${set_secret} $vault_name "${secret_base_name}-id" "$spid" || return $?
    ${set_secret} $vault_name "${secret_base_name}-appid" "$appid" || return $?
    ${set_secret} $vault_name "${secret_base_name}-name" "$name" || return $?
    ${set_secret} $vault_name "${secret_base_name}-tenant" "$tenant" || return $?
    if [ ! -z $password ]; then
        ${set_secret} $vault_name "${secret_base_name}-password" $password $expires || return $?
    fi

    # track the object ids for later
    echo_verbose "Setting principal_objectids and principal_appids"
    principal_objectids[${sp_name}]=$spid
    echo_debug "Service principal id ${sp_name} : ${principal_objectids[$sp_name]}"
    principal_appids[${sp_name}]=$appid
    echo_debug "Service principal appid ${sp_name} : ${principal_appids[$sp_name]}"
}

function az_keyvault_set_policy_service_principal_list_get()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local base_name="${1}"
    local keyvault_name="${2}"
    local secret_permissions="list get"
    local certificate_permissions="list get"
    local sp_name="$(get_sp_name ${base_name})"
    local sp_object_id="${principal_objectids[$sp_name]}"

    if [ -z "${sp_object_id}" ]; then
        echo_error "Did not find object id for '${sp_name}'"
        return 1
    fi

    echo_info "Adding list and get permissions to keyvault '${keyvault_name}' for service principal '${sp_name}'"
    local az_command="az keyvault set-policy --name ${keyvault_name} --object-id ${sp_object_id} --secret-permissions ${secret_permissions} --certificate-permissions ${certificate_permissions} --query id"
    exec_dry_run "${az_command}"
}

function az_keyvault_set_policy()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local keyvault_name="${1}"
    local assignee_display_name="${2}"
    local assignee_object_id="${3}"
    local secret_permissions="${4}"
    local certificate_permissions="${5}"
    local az_command="az keyvault set-policy --name ${keyvault_name} --object-id ${assignee_object_id} --secret-permissions ${secret_permissions} --certificate-permissions ${certificate_permissions} --query id"

    echo_info "Adding permissions to keyvault '${keyvault_name}' for '${assignee_display_name} (${assignee_object_id})'"
    exec_dry_run "${az_command}"
}

function az_role_assignment_create()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local assignee="${1}"
    local assignee_name="${2}"
    local role="${3}"
    local scope="${4}"

    echo_info "Checking that '${assignee_name}' has role '${role}' in scope '${scope}'"
    local az_exists_command="az role assignment list --assignee ${assignee} --scope ${scope} --role ${role} --include-groups --query id --output tsv"
    local assignments="$(exec_dry_run_with_stdout "-" "${az_exists_command}")" || return $?
    [ "${assignments}" = '-' ] && assignments=""

    if [ -z "${assignments}" ]; then
        echo_info "Granting '${assignee_name}' role '${role}' in scope '${scope}'"
        local az_command="az role assignment create --assignee ${assignee} --scope ${scope} --role ${role} --query id --output tsv" || return $?
        exec_dry_run "${az_command}"
    fi
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

    az_role_assignment_create ${appId} ${sp_name} ${role} ${scope}
}

function az_role_assignment_create_scope_acrpull()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local base_name="${1}"
    local resource_group="${2}"
    local registry_name="${3}"
    local role="acrpull"

    local sp_name="$(get_sp_name ${base_name})"
    local appId="${principal_appids[$sp_name]}"
    local scope="/subscriptions/${subscription_id}/resourceGroups/${resource_group}/providers/Microsoft.ContainerRegistry/registries/${registry_name}"

    az_role_assignment_create ${appId} ${sp_name} ${role} ${scope}
}

function az_role_assignment_create_scope_acrpull_for_computeService()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local subscription_dev_id="86642df6-843e-4610-a956-fdd497102261"
    local base_name="app"
    local resource_group="vsclk-core-dev"
    local registry_name="vsclkapps"
    local role="acrpull"

    local sp_name="$(get_sp_name ${base_name})"
    local appId="${principal_appids[$sp_name]}"
    local scope="/subscriptions/${subscription_dev_id}/resourceGroups/${resource_group}/providers/Microsoft.ContainerRegistry/registries/${registry_name}"

    az_role_assignment_create ${appId} ${sp_name} ${role} ${scope}
}

function az_aks_get_credentials()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo_info "Getting the AKS credentials for ${cluster_name}"
    local cluster_name="${1}"
    local cluster_rg="${2}"
    local az_command="az aks get-credentials --resource-group $cluster_rg --name $cluster_name --overwrite-existing"
    exec_dry_run "$az_command"
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
    echo_info "Azure subscription: $subscription_name ($subscription_id)"
}

function az_trafficmanager_exists()
{
    local resource_group="${1}"
    local profile_name="${2}"
    local az_command="az network traffic-manager profile show --resource-group ${resource_group} --name ${profile_name}"

    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_trafficmanager_endpoint_exists()
{
    local resource_group="${1}"
    local profile_name="${2}"
    local endpoint_name="${3}"
    local az_command="az network traffic-manager endpoint show --resource-group ${resource_group} --profile-name ${profile_name} --name ${endpoint_name} --type externalEndpoints"

    if get_dry_run; then
        echo_verbose "${az_command}"
        return 1
    fi
    exec_dry_run "${az_command}" 2> /dev/null > /dev/null
}

function az_trafficmanager_endpoint_create_or_update()
{
    local resource_group="${1}"
    local profile_name="${2}"
    local endpoint_name="${3}"
    local endpoint_location="${4}"
    local endpoint_target="$(set +u; echo ${5})"
    local endpoint_target_id="$(set +u; echo ${6})"
    local verb="update"
    local type="externalEndpoints"
    local location_arg=""
    local target_arg=""

    if [ $validate_only -eq 1 ]; then
        return 0
    fi

    if [ ! -z $endpoint_target ]; then
        local type="externalEndpoints"
        local min_child_endpoints_arg=""
        target_arg="--target ${endpoint_target}"
    else
        local type="nestedEndpoints"
        local min_child_endpoints_arg="--min-child-endpoints 1"
        target_arg="--target-resource-id ${endpoint_target_id}"
    fi

    if ! az_trafficmanager_endpoint_exists $resource_group $profile_name $endpoint_name; then
        verb="create"
        location_arg="--endpoint-location ${endpoint_location}"
    fi

    local az_command="az network traffic-manager endpoint $verb --resource-group ${resource_group} --profile-name ${profile_name} --name ${endpoint_name} --type ${type} ${min_child_endpoints_arg} ${target_arg} ${location_arg}"

    echo_info "Setting traffic manager endpoint ${profile_name}/${endpoint_name} to ${endpoint_target}${endpoint_target_id}"
    exec_dry_run "${az_command}"
}

function kubectl_apply()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local file="${1}"
    local kubectl_command="kubectl apply -f $file"
    exec_dry_run "$kubectl_command"
}

function kubectl_apply_withNamespace()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local filename="${1}"
    local namespace="${2}"
    local kubectl_command="kubectl apply -n $namespace -f $filename"
    exec_dry_run "$kubectl_command"
}

function kubectl_get_ip_address()
{
    if get_dry_run; then
        echo "0.0.0.0"
        return 0
    fi
    local serviceName="${1}"
    local command="kubectl get service ${serviceName} -o=jsonpath='{.status.loadBalancer.ingress[0].ip}'"
    $command
}

function generate_parameters()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"

    echo_info "Generating cluster parameters files"

    # Transform regions to azure region names for ARM templates (e.g. usw2 -> westus2)
    local azure_regions="$(region_shortcodes_to_azure_locations)"

    # parameters require object ids
    set_ad_object_ids || return $?

    echo_info "Subscription id: $subscription_id"

    exec_verbose . "${script_dir_1753}/generate-cluster-parameters.sh" \
        --subscription-id="$subscription_id" \
        --prefix="$prefix" \
        --name="$name" \
        --env="$env" \
        --instance="$instance" \
        --stamp="$stamp" \
        --regions="$azure_regions" \
        --keyvault-resource-group="$keyvault_rg" \
        --keyvault-name="$keyvault_name" \
        --app-sp-id="$app_sp_objectid" \
        --sts-sp-id="$sts_sp_objectid" \
        --devops-sp-id="$devops_sp_objectid" \
        --team-id="$team_group_id" \
        --user-id="$user_objectid" \
        --dns-name="$dns_name" \
        --cluster-version="$cluster_version" \
        --cluster-node-count="$cluster_node_count" \
        --signlr-enabled="$signlr_enabled" \
        --signlr-capacity="$signlr_capacity" \
        --signlr-capacity-sec="$signlr_capacity_sec"
}

function az_ad_signed_in_user_object_id()
{
    if get_dry_run; then
        echo "dry-run-user"
        return 0
    fi

    # this fails if the sign-in user is a service principal
    az ad signed-in-user show --query objectId -o tsv || echo ""
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

    echo_verbose "getting app service principal object id"
    local sp_name=$(get_sp_name "app")
    app_sp_objectid="${principal_objectids[$sp_name]}"
    echo_debug "app_sp_objectid: ${app_sp_objectid}"

    if [[ "${signlr_enabled}" != "true" ]] ; then
        echo_verbose "getting sts service principal object id"
        local sp_name=$(get_sp_name "sts")
        sts_sp_objectid="${principal_objectids[$sp_name]}"
        echo_debug "sts_sp_objectid: ${sts_sp_objectid}"
    else
        sts_sp_objectid=""
    fi

    echo_verbose "getting devops service principal object id"
    local sp_name=$(get_sp_name "devops")
    devops_sp_objectid="${principal_objectids[$sp_name]}"
    echo_debug "devops_sp_objectid: ${devops_sp_objectid}"
}

function apply_default_cluster_configuration()
{
    local kube_system="kube-system"
    local default="default"
    echo_info "Applying K8s default cluster configuration ($cluster_name) to namespaces $kube_system and $default"
    # Tiller needs to be installed on both namespaces 'default' and 'kube-system'

    kubectl_apply_withNamespace "$k8s_dir/custom-default-psp.yml" $default || return $?
    kubectl_apply_withNamespace "$k8s_dir/custom-default-psp-role.yml" $default || return $?
    kubectl_apply_withNamespace "$k8s_dir/custom-default-psp-rolebinding.yml" $default || return $?

    kubectl_apply_withNamespace "$k8s_dir/custom-default-psp.yml" $kube_system || return $?
    kubectl_apply_withNamespace "$k8s_dir/custom-default-psp-role.yml" $kube_system || return $?
    kubectl_apply_withNamespace "$k8s_dir/custom-default-psp-rolebinding.yml" $kube_system || return $?
    kubectl_apply_withNamespace "$k8s_dir/kubernetes-dashboard-clusterrolebinding.yml" $kube_system || return $?

    kubectl_apply_withNamespace "$k8s_dir/tiller-sa-k-system.yml" $kube_system || return $?
    kubectl_apply_withNamespace "$k8s_dir/tiller-sa-clusterrolebinding-k-system.yml" $kube_system || return $?

    kubectl_apply_withNamespace "$k8s_dir/tiller-sa-default.yml" $default || return $?
    kubectl_apply_withNamespace "$k8s_dir/tiller-sa-clusterrolebinding-default.yml" $default || return $?
}

function init_cluster() 
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
 
    local cluster_name="${1}"
    local cluster_rg="${2}"
    local instance_tm="${3}"
    echo_info "Initializing cluster ${cluster_name}"

    # Connect kubectl to the AKS cluster
    az_aks_get_credentials ${cluster_name} ${cluster_rg} || return $?
    exec_dry_run "kubectl config current-context"  || return $?

    # Configure the cluster
    apply_default_cluster_configuration || return $?
    apply_all_cluster_ssl_certificates || return $?

    # Ensure helm is installed. Note that version mismatch between the client and server is a big deal.
    # The --force-upgrade switch will force the service to upgrade OR downgrade to the client version.
    # Without --force-upgrade, simple --upgrade fails if the service is a higher version :(
    # The version used on the Azure DevOps pipeline job agent will end up winning over time.
    # We are purposefully installing tiller on 'kube-system' and 'default' namespaces
    # This allows us to use helm to install charts on both namespaces
    echo_info "Installing helm-tiller to kube-system namespace"
    exec_dry_run helm init --service-account "tiller-sa" --tiller-namespace kube-system --upgrade --force-upgrade --wait || return $?
    echo_info "Installing helm-tiller to default namespace"
    exec_dry_run helm init --service-account "tiller-sa-default" --tiller-namespace default --upgrade --force-upgrade --wait || return $?

    # Set a long timeout because pipelines sometimes fail with default 5m timeout (300)
    local helm_upgrade_timeout="600" # 10m

    # special-case signalr values file name
    local values_file_suffix="" && [[ "${signlr_enabled}" = "true" ]] && values_file_suffix=".signalr"

    # linux-geneva chart
    echo_info "Installing linux-geneva (AzSecPac, etc) to kube-system"
    local install_name="linux-geneva"
    local namespace="kube-system"
    exec_dry_run helm upgrade ${install_name} "${charts_dir}/${install_name}" --install --set image.repositoryUrl=${registry_name}.azurecr.io -f "${charts_dir}/valuefiles/values.${env}${values_file_suffix}.yaml" --namespace ${namespace} --tiller-namespace ${namespace} --force --timeout ${helm_upgrade_timeout} || return $?

    # geneva logger chart
    echo_info "Deleting geneva-logger (log forwarder for pod telemetry) from kube-system and re-installing"
    # The delete ensures any updates to the configuration files are applied when the pods restart
    local install_name="geneva-logger"
    local namespace="kube-system"
    exec_dry_run helm delete --tiller-namespace ${namespace} ${install_name} --purge || true
    exec_dry_run helm upgrade ${install_name} "${charts_dir}/${install_name}" --install --set image.repositoryUrl=${registry_name}.azurecr.io -f "${charts_dir}/valuefiles/values.${env}${values_file_suffix}.yaml" --namespace ${namespace} --tiller-namespace ${namespace} --force --timeout ${helm_upgrade_timeout} || return $?

    # linux-common chart
    echo_info "Installing linux-common (nginx) to default with load balancer and kured (OS management) to kube-system"
    local install_name="linux-common"
    local namespace="default"
    local setIngressReplicaCount=""
    if [ "${instance_name}" == "vsclk-online-dev-stg" ]; then
        setIngressReplicaCount="--set ingress.replicaCount=1"
    fi
    exec_dry_run helm upgrade ${install_name} "${charts_dir}/${install_name}" --install --set image.repositoryUrl=${registry_name}.azurecr.io ${setIngressReplicaCount} --set clusterName=${cluster_name} --namespace ${namespace} --tiller-namespace ${namespace} --wait --force --timeout ${helm_upgrade_timeout} --debug || return $?

    # Update the cluster IP address in the cluster traffic manager.
    # The Azure Front Door backend points to this traffic manager.
    cluster_ip=$(kubectl_get_ip_address "service-frontend-lb")
    cluster_ip="${cluster_ip//\'/}"
    echo_info "Cluster ip address: ${cluster_ip}"
    az_trafficmanager_endpoint_create_or_update ${instance_rg} ${instance_tm} ${stamp}-ip ${cluster_location} ${cluster_ip} || return $?

}

function az_download_and_split_ssl_certificate()
{
    # expects the a filename like foo.pfx
    # generates output file names like foo.cer, foo.key, and foo.rsa.key
    local pfx_file="${1}"
    local secret_name="${2}"
    local base_name=$(basename ${pfx_file} .pfx)
    local cer_file="${base_name}.cer"
    local key_file="${base_name}.key"
    local rsa_key_file="${base_name}.rsa.key"
    local az_command="az keyvault secret download --vault-name ${ssl_cert_kv_name} --name ${secret_name} --encoding base64 --file ${pfx_file}"
    exec_dry_run "${az_command}" || return $?

    local passwordplaintext=""
    exec_dry_run "openssl pkcs12 -in $pfx_file -out $cer_file -nokeys -clcerts -password pass:$passwordplaintext" || return $?
    exec_dry_run "openssl pkcs12 -in $pfx_file -out $key_file -nocerts -nodes -password pass:$passwordplaintext" || return $?
    exec_dry_run "openssl rsa -in $key_file -out $rsa_key_file" || return $?

    # Download and append the intermediate certificate
    if ! get_dry_run; then
        local certScheme="$(openssl x509 -in $cer_file -noout -text | grep "CA Issuers" | cut -d':' -f2)"
        local certHostAndPath="$(openssl x509 -in $cer_file -noout -text | grep "CA Issuers" | cut -d':' -f3)"
        local intermediateCertUri="${certScheme}:${certHostAndPath}"
    else
        local intermediateCertUri="http://dry-run/cert.crt"
    fi
    if [ ! "$intermediateCertUri" == ":" ]; then
        echo_info "Getting the intermediate cert from ${intermediateCertUri}"
        if get_dry_run; then
            exec_dry_run "curl -s ${intermediateCertUri} | openssl x509 -inform der"
        else
            echo_verbose "curl -s ${intermediateCertUri} | openssl x509 -inform der"
            (curl -s ${intermediateCertUri} | openssl x509 -inform der) >> $cer_file
            echo_verbose "$(cat $cer_file)"
        fi
    else
        echo_warning "No intermediate certificate uri found for certificate ${secret_name}"
    fi
}

function apply_cluster_ssl_certificate()
{
    local kv_secret_name="${1}"
    local kube_secret_name="${2}"

    echo_info "Downloading SSL certificate"
    if [ -f "ssl.pfx" ]; then
       exec_dry_run "rm ssl.*"
    fi
    az_download_and_split_ssl_certificate "ssl.pfx" "${kv_secret_name}" || return $?
    echo_info "Creating the ${kube_secret_name} secret in the cluster"

	if get_dry_run; then
		exec_dry_run "kubectl create secret tls ${kube_secret_name} --cert=ssl.cer --key=ssl.key --dry-run -o yaml | kubectl apply -f -"
	else
		echo_verbose "kubectl create secret tls ${kube_secret_name} --cert=ssl.cer --key=ssl.key --dry-run -o yaml | kubectl apply -f -"
		kubectl create secret tls ${kube_secret_name} --cert=ssl.cer --key=ssl.key --dry-run -o yaml | kubectl apply -f - || return $?
	fi

    if [ -f "ssl.pfx" ]; then
       exec_dry_run "rm ssl.*"
    fi
}

function apply_all_cluster_ssl_certificates()
{
    # Format is "deployed-name-1:keyvault-name-1,deployed-name-2:keyvault-name-2,..."
    split_string cert_arr "${ssl_cert_secrets}" ","

    for cert_item in "${cert_arr[@]}"
    do
        echo_verbose "Applying ssl cert '$cert_item'"
        split_string cert_key_value "${cert_item}" ":"
        apply_cluster_ssl_certificate "${cert_key_value[1]}" "${cert_key_value[0]}" || return $?
    done
}

# Script Variables
src_dir="$( cd "${script_dir_1753}/../.." >/dev/null 2>&1 && pwd )"
template_dir="${src_dir}/arm"
parameters_dir="${template_dir}/parameters"
k8s_dir="${src_dir}/k8s"
charts_dir="${src_dir}/charts"
powershell_scripts_dir="${src_dir}/scripts/powershell"
azurebatch_scripts_dir="${src_dir}/scripts/azurebatch"
environment_name="${prefix}-${name}-${env}"
instance_name="${prefix}-${name}-${env}-${instance}"
environment_rg="${environment_name}"
instance_rg="${instance_name}"
instance_tm="${instance_name}-tm"
pf_instance_tm="${prefix}-${port_forwarding_service_name}-${env}-${instance}-tm"
keyvault_rg="${environment_rg}"
keyvault_name="${environment_name}-kv"
sts_keyvault_name="${prefix}-core-${env}-sts-kv"
billing_keyvault_name="${prefix}-core-${env}-pav2-kv"
registry_rg="${environment_rg}"
registry_name="${environment_name//-/}"
cluster_rg="${instance_rg}-${stamp}"
cluster_name="${cluster_rg}-cluster"
pf_cluster_name="${prefix}-${port_forwarding_service_name}-${env}-${instance}-${stamp}-cluster"
instance_images_rg="${environment_name}-images-${stamp}"

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
echo_debug "  powershell_scripts_dir: ${powershell_scripts_dir}"
echo_debug "  azurebatch_scripts_dir: ${azurebatch_scripts_dir}"
echo_debug "  environment_name: ${environment_name}"
echo_debug "  instance_name: ${instance_name}"
echo_debug "  environment_rg: ${environment_rg}"
echo_debug "  instance_rg: ${instance_rg}"
echo_debug "  keyvault_rg: ${keyvault_rg}"
echo_debug "  keyvault_name: ${keyvault_name}"
echo_debug "  billing_keyvault_name: ${billing_keyvault_name}"
echo_debug "  cluster_rg: ${cluster_rg}"
echo_debug "  cluster_name: ${cluster_name}"
echo_debug "  deployment_action: $deployment_action"
echo_debug "  deployment_verb: $deployment_verb"


function echo_success() {
    echo_info "Success"
}

# A very simple function to get a value given a filepath and yml key
function yml_get_value() {
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    local yaml_filepath="${1}"
    local yaml_key="${2}"
    local yaml_value=$(grep "$yaml_key:" $yaml_filepath | awk '{ print $2}' | tr -d '"')
    if [ -z "$yaml_value" ]
    then
        echo_error "Unable to find key $yaml_key in $yaml_filepath or value is empty."
        return 1
    else
        echo "$yaml_value"
    fi
}

function main()
{
    echo_debug "${script_name_1753}::${FUNCNAME[0]} $@"
    echo
    echo_info "${script_name_1753} --stage ${stage} --stamp ${stamp}"
    echo

    # Set the Azure subscription
    az_account_set || return $?

    # Everything depends on the base resoruce and service principals
    if stageBootstrap; then
        init_cluster_envrionment_base_resources || return $?
        echo_success
        return 0
    fi

    # Generate ARM parameters files
    # The ARM templates needs the service principal appids
    if stageCommon || stageStamp; then
        ensure_service_principals || return $?
        ensure_aes_private_keys || return $?
        generate_parameters || return $?
    fi

    # Deploy environment and instance ARM templates
    if stageCommon; then
        deploy_cluster_environment_resources || return $?
        deploy_cluster_instance_resources || return $?
        echo_success
        return 0
    fi

    # Deploy stamp ARM templates
    if stageStamp; then
        deploy_cluster_stamp_resources || return $?
        echo_success
        return 0
    fi

    if stageCluster; then
        init_cluster ${cluster_name} ${cluster_rg} ${instance_tm} || return $?

        if [[ "${port_forwarding_enabled}" = "true" ]] ; then
            init_cluster ${pf_cluster_name} ${cluster_rg} ${pf_instance_tm} || return $?
        fi

        echo_success
        return 0
    fi

    # TODO
    # - DNS CNAMEs names
}

main
