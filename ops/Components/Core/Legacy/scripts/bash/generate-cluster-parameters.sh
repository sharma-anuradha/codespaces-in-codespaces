#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Import utilities
# Script PIN 8201
script_name_8201="$( basename "${BASH_SOURCE}" )"
script_dir_8201="$( cd "$( dirname "${BASH_SOURCE}" )" >/dev/null 2>&1 && pwd )"
. "${script_dir_8201}/utilities.sh"
echo_debug "Script 8201 name ${script_name_8201}"
echo_debug "Script 8210 dir ${script_dir_8201}"

# Script parameters
# An option followed by a single colon ':' means that it *needs* an argument.
# An option followed by double colons '::' means that its argument is optional.

add_option "subscription-id:" "s:"
subscription_id=

add_option "prefix:" "p:"
prefix=

add_option "name:" "n:"
name=

add_option "env:" "e:"
env=

add_option "instance:" "i:"
instance=

add_option "stamp:" "m:"
stamp=

add_option "regions:" "r:"
regions=

add_option "keyvault-resource-group:"
keyvault_rg=

add_option "keyvault-name:"
keyvault_name=

add_option "app-sp-id:"
app_sp_objectid=

add_option "sts-sp-id:"
sts_sp_objectid=

add_option "devops-sp-id:"
devops_sp_objectid=

add_option "team-id:"
team_objectid=

add_option "user-id:"
user_objectid=

add_option "dns-name:"
dns_name=

add_option "cluster-version:"
cluster_version=

add_option "cluster-node-count:"
cluster_node_count=

add_option "signlr-enabled:"
signlr_enabled="true"

add_option "signlr-capacity:"
signlr_capacity=

add_option "signlr-capacity-sec:"
signlr_capacity_sec=

add_option "help" "h"
help=0
add_option "verbose" "v"
add_option "debug" "d"

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
        -s|--subscription-id) shift
            subscription_id="$1";;
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
        -r|--regions) shift
            regions="$1";;
        --keyvault-resource-group) shift
            keyvault_rg="$1";;
        --keyvault-name) shift
            keyvault_name="$1";;
        --app-sp-id) shift
            app_sp_objectid="$1";; 
        --sts-sp-id) shift
            sts_sp_objectid="$1";; 
        --devops-sp-id) shift
            devops_sp_objectid="$1";; 
        --team-id) shift
            team_objectid="$1";; 
        --user-id) shift
            user_objectid="$1";; 
        --dns-name) shift
            dns_name="$1";;
        --cluster-version) shift
            cluster_version="$1";;
        --cluster-node-count) shift
            cluster_node_count="$1";;
        --signlr-enabled) shift
            signlr_enabled="$1";;
        --signlr-capacity) shift
            signlr_capacity="$1";;
        --signlr-capacity-sec) shift
            signlr_capacity_sec="$1";;
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

#decode object type parameters encoded as base64
cluster_node_count=$(echo -n "${cluster_node_count}" | base64 --decode)
signlr_capacity=$(echo -n "${signlr_capacity}" | base64 --decode)
signlr_capacity_sec=$(echo -n "${signlr_capacity_sec}" | base64 --decode)

# Debug output for parameters
echo_debug "Parameters: "
echo_debug "  short options: ${options}"
echo_debug "  long options: ${longoptions}"
echo_debug "  subscription_id: ${subscription_id}"
echo_debug "  prefix: ${prefix}"
echo_debug "  name: ${name}"
echo_debug "  env: ${env}"
echo_debug "  instance: ${instance}"
echo_debug "  stamp: ${stamp}"
echo_debug "  regions: ${regions}"
echo_debug "  keyvault_rg: ${keyvault_rg}"
echo_debug "  keyvault_name: ${keyvault_name}"
echo_debug "  app_sp_objectid: ${app_sp_objectid}"
echo_debug "  sts_sp_objectid: ${sts_sp_objectid}"
echo_debug "  devops_sp_objectid: ${devops_sp_objectid}"
echo_debug "  team_objectid: ${team_objectid}"
echo_debug "  user_objectid: ${user_objectid}"
echo_debug "  dns_name: ${dns_name}"
echo_debug "  cluster_version: ${cluster_version}"
echo_debug "  cluster_node_count: ${cluster_node_count}"
echo_debug "  signlr_enabled: ${signlr_enabled}"
echo_debug "  signlr_capacity: ${signlr_capacity}"
echo_debug "  signlr_capacity_sec: ${signlr_capacity_sec}"
echo_debug "  help: ${help}"
echo_debug "  verbose: ${utilities_verbose}"
echo_debug "  debug: ${utilities_debug}"

if [ $help != 0 ]; then
    echo "usage $(basename $0) [options]"
    echo " -s, --subscription-id     the subscription id"
    echo " -p, --prefix              the service name prefix, for example, 'vsclk'"
    echo " -n, --name                the service base name"
    echo " -e, --env                 the service environment, 'dev', 'ppe', 'prod', 'rel'"
    echo " -i, --instance            the service instance name, defaults to environment name"
    echo "     --dns-name            the service instance cluster dns name"
    echo " -m, --stamp               the service stamp name"
    echo " -r, --regions             the regions where the service is deployed (Azure region names)"
    echo "     --cluster-version     the Kubernetes cluster version to use"
    echo "     --cluster-node-count  the number of nodes in the cluster for each region (json-formatted base64-encoded string, ex:'{use:2, euw:3}'->'e3VzZToyLCBldXc6M30K')"
    echo "     --keyvault-resource-group"
    echo "                           the keyvault resource group"
    echo "     --keyvault-name       the keyvault name"
    echo "     --app-sp-id           the application service principal id"
    echo "     --sts-sp-id           the token service principal id"
    echo "     --devops-sp-id        the devops service principal id"
    echo "     --team-id             the team ad group id"
    echo "     --user-id             the current user ad user id"
    echo "     --signlr-enabled      indicates whether to deploy signalR resource"
    echo "     --signlr-capacity     the capacity of the primary signalR resource"
    echo "     --signlr-capacity-sec the capacity of the secondary signalR resource"
    echo " -v, --verbose             emit verbose info"
    echo " -d, --debug               emit debug info"
    echo " -h, --help                show help"
    exit 0
fi

if [[ -z ${subscription_id} ]] ; then
    echo_error "Parameter --subscription-id (-s) is required" && exit 1
fi
if [[ -z ${prefix} ]] ; then
    echo_error "Parameter --prefix (-p) is required" && exit 1
fi
if [[ -z ${name} ]] ; then
    echo_error "Parameter --name (-n) is required" && exit 1
fi
if [[ -z ${env} ]] ; then
    echo_error "Parameter --env (-e) is required" && exit 1
fi
if [[ -z ${instance} ]] ; then
    instance=$env
    echo_warning "Parameter --instance (-i) is not specified, using '${instance}'"
fi
if [[ -z ${stamp} ]] ; then
    echo_error "Parameter --stamp (-m) is required" && exit 1
fi
if [[ -z ${regions} ]] ; then
    echo_error "Parameter --regions (-r) is required" && exit 1
fi
if [[ -z ${keyvault_rg} ]] ; then
    keyvault_rg="${prefix}-${name}-${env}"
    echo_warning "Parameter --keyvault-resource-group is not specified, using '${keyvault_rg}'"
fi
if [[ -z ${keyvault_name} ]] ; then
    keyvault_name="${keyvault_rg}-kv"
    echo_warning "Parameter --keyvault-name is not specified, using '${keyvault_name}'"
fi
if [[ -z ${app_sp_objectid} ]] ; then
    echo_error "Parameter --app-sp-id is required" && exit 1
fi
if [[ -z ${sts_sp_objectid} ]] ; then
    if [[ "${signlr_enabled}" != "true" ]] ; then
        echo_error "Parameter --sts-sp-id is required" && exit 1
    fi
fi
if [[ -z ${devops_sp_objectid} ]] ; then
    echo_error "Parameter --devops-sp-id is required" && exit 1
fi
if [[ -z ${team_objectid} ]] ; then
    echo_error "Parameter --team-id is required" && exit 1
fi
if [[ -z ${dns_name} ]] ; then
    echo_error "Parameter --dns-name is required" && exit 1
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
    else
        signlr_capacity="0"
    fi
fi
if [[ -z ${signlr_capacity_sec} ]] ; then
    if [[ "${signlr_enabled}" = "true" ]] ; then
        echo_error "Parameter --signlr-capacity-sec is required" && exit 1
    else
        signlr_capacity_sec="0"
    fi
fi

# Script Variables
echo_debug "script_dir_8201=$script_dir_8201"
parameters_dir="$( cd "$script_dir_8201/../../arm/parameters" >/dev/null 2>&1 && pwd )"
echo_debug "parameters_dir: $parameters_dir"
parameters_templates_dir="$( cd "$script_dir_8201/../../arm/parameters-templates" >/dev/null 2>&1 && pwd )"
echo_debug "parameters_templates_dir: $parameters_templates_dir"

echo_debug "Script variables:"
echo_debug "  parameters_dir=${parameters_dir}"
echo_debug "  parameters_templates_dir=${parameters_templates_dir}"

dump_environment_vars()
{
    echo_debug "${script_name_8201}::${FUNCNAME[0]} $@"
    echo_verbose "Substitution environment variables:"
    echo_verbose "  SERVICE_SUBSCRIPTION=$SERVICE_SUBSCRIPTION"
    echo_verbose "  SERVICE_PREFIX=$SERVICE_PREFIX"
    echo_verbose "  SERVICE_NAME=$SERVICE_NAME"
    echo_verbose "  SERVICE_ENVIRONMENT=$SERVICE_ENVIRONMENT"
    echo_verbose "  SERVICE_INSTANCE=$SERVICE_INSTANCE"
    echo_verbose "  SERVICE_STAMP=$SERVICE_STAMP"
    echo_verbose "  SERVICE_REGIONS=$SERVICE_REGIONS"
    echo_verbose "  SERVICE_KEYVAULT_RESOURCE_GROUP=$SERVICE_KEYVAULT_RESOURCE_GROUP"
    echo_verbose "  SERVICE_KEYVAULT_NAME=$SERVICE_KEYVAULT_NAME"
    echo_verbose "  SERVICE_APP_SP_OBJECTID=$app_sp_objectid"
    echo_verbose "  SERVICE_STS_SP_OBJECTID=$sts_sp_objectid"
    echo_verbose "  SERVICE_DEVOPS_SP_OBJECTID=$devops_sp_objectid"
    echo_verbose "  SERVICE_TEAM_OBJECTID=$team_objectid"
    echo_verbose "  SERVICE_USER_OBJECTID=$user_objectid"
    echo_verbose "  SERVICE_DNS_NAME=$dns_name"
    echo_verbose "  SERVICE_CLUSTER_VERSION=$cluster_version"
    echo_verbose "  SERVICE_CLUSTER_NODE_COUNT=$cluster_node_count"
    echo_verbose "  SERVICE_SIGNLR_ENABLED=$signlr_enabled"
    echo_verbose "  SERVICE_SIGNLR_CAPACITY=$signlr_capacity"
    echo_verbose "  SERVICE_SIGNLR_CAPACITY_SEC=$signlr_capacity_sec"   
}

set_environment_vars()
{
    echo_debug "${script_name_8201}::${FUNCNAME[0]} $@"
    export SERVICE_SUBSCRIPTION=$subscription_id
    export SERVICE_PREFIX=$prefix
    export SERVICE_NAME=$name
    export SERVICE_ENVIRONMENT=$env
    export SERVICE_INSTANCE=$instance
    export SERVICE_STAMP=$stamp
    export SERVICE_REGIONS=$regions
    export SERVICE_KEYVAULT_RESOURCE_GROUP=$keyvault_rg
    export SERVICE_KEYVAULT_NAME=$keyvault_name
    export SERVICE_APP_SP_OBJECTID=$app_sp_objectid
    export SERVICE_STS_SP_OBJECTID=$sts_sp_objectid
    export SERVICE_DEVOPS_SP_OBJECTID=$devops_sp_objectid
    export SERVICE_TEAM_OBJECTID=$team_objectid
    export SERVICE_USER_OBJECTID=$user_objectid
    export SERVICE_DNS_NAME=$dns_name
    export SERVICE_CLUSTER_VERSION=$cluster_version
    export SERVICE_CLUSTER_NODE_COUNT=$cluster_node_count
    export SERVICE_SIGNLR_ENABLED=$signlr_enabled
    export SERVICE_SIGNLR_CAPACITY=$signlr_capacity
    export SERVICE_SIGNLR_CAPACITY_SEC=$signlr_capacity_sec
}

function pre_process_parameter_json()
{
    echo_debug "${script_name_8201}::${FUNCNAME[0]} $@"
    echo_verbose "Pre-processing ${f}"
    
    filename="$(basename "${f}")"
    input_filename="${f}"
    output_filename="$parameters_dir/$filename"
    if [ -f "${output_filename}" ]; then
        rm "${output_filename}"
    fi
    envsubst < $input_filename > $output_filename
    echo_info "ARM parameters generated from '${input_filename}' to '${output_filename}'"
    echo_verbose "${output_filename}:\n$(cat "${output_filename}")"

    return 0
}

function main()
{
    echo_debug "${script_name_8201}::${FUNCNAME[0]} $@"
    set_environment_vars || return $?
    dump_environment_vars

    last_error=0
    for f in ${parameters_templates_dir}/*.parameters.json; do
        [ -f "${f}" ] || break
        pre_process_parameter_json "${f}"
        last_error=$?
        if [ ! ${last_error} -eq 0 ]; then
            break;
        fi 
    done
    return $last_error
}

main
