#!/bin/bash
# renormalize

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Import utilities
. "utilities.sh"

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

add_option "keyvault-resource-group:"
keyvault_rg=

add_option "keyvault-name:"
keyvault_name=

add_option "keyvault-reader-object-id:"
keyvault_reader_objectid=

add_option "keyvault-reader-tenant-id:"
keyvault_reader_tenantid=

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
        --keyvault-resource-group) shift
            keyvault_rg="$1";;
        --keyvault-name) shift
            keyvault_name="$1";;
        --keyvault-reader-object-id) shift
            keyvault_reader_objectid="$1";;
        --keyvault-reader-tenant-id) shift
            keyvault_reader_tenantid="$1";;
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
echo_debug "  short options: $options"
echo_debug "  long options: $longoptions"
echo_debug "  subscription: $subscription"
echo_debug "  prefix: $prefix"
echo_debug "  name: $name"
echo_debug "  env: $env"
echo_debug "  instance: $instance"
echo_debug "  keyvault_rg: $keyvault_rg"
echo_debug "  keyvault_name: $keyvault_name"
echo_debug "  keyvault_reader_objectid: $keyvault_reader_objectid"
echo_debug "  keyvault_reader_tenantid: $keyvault_reader_tenantid"
echo_debug "  verbose: ${verbose}"
echo_debug "  debug: ${debug}"
echo_debug "  help: ${help}"

if [ $help != 0 ]; then
    echo "usage $(basename $0) [options]"
    echo " -s, --subscription-id  the subscription id"
    echo " -p, --prefix           the service name prefix, for example, 'vsclk'"
    echo " -n, --name             the service base name"
    echo " -e, --env              the service environment, 'dev', 'ppe', 'prod', 'rel'"
    echo " -i, --instance         the service instance name, defaults to environment name"
    echo "     --keyvault-resource-group"
    echo "                        the keyvault resource group"
    echo "     --keyvault-name    the keyvault name"
    echo "     --keyvault-reader-object-id"
    echo "                        the keyvault reader service principal object id"
    echo "     --keyvault-reader-tenant-id"
    echo "                        the keyvault reader service principal tenant id"
    echo " -v, --verbose          emit verbose info"
    echo " -d, --debug            emit debug info"
    echo " -h, --help             show help"
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
if [[ -z ${keyvault_rg} ]] ; then
    keyvault_rg="${prefix}-${name}-${env}"
    echo_warning "Parameter --keyvault-resource-group is not specified, using '${keyvault_rg}'"
fi
if [[ -z ${keyvault_name} ]] ; then
    keyvault_name="${keyvault_rg}-kv"
    echo_warning "Parameter --keyvault-name is not specified, using '${keyvault_name}'"
fi
if [[ -z ${keyvault_reader_objectid} ]] ; then
    echo_error "Parameter --keyvault-reader-object-id is required" && exit 1
fi
if [[ -z ${keyvault_reader_tenantid} ]] ; then
    echo_error "Parameter --keyvault-reader-tenant-id is required" && exit 1
fi

# Script Variables
dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
echo_debug "dir=$dir"
parameters_dir="$( cd "$dir/../../arm/parameters" >/dev/null 2>&1 && pwd )"
echo_debug "parameters_dir: $parameters_dir"
parameters_templates_dir="$( cd "$dir/../../arm/parameters-templates" >/dev/null 2>&1 && pwd )"
echo_debug "parameters_templates_dir: $parameters_templates_dir"

echo_debug "Script variables:"
echo_debug "  parameters_dir=${parameters_dir}"
echo_debug "  parameters_templates_dir=${parameters_templates_dir}"

dump_environment_vars()
{
    echo_debug "$0::${FUNCNAME[0]}"
    echo_verbose "Substitution environment variables:"
    echo_verbose "  SERVICE_SUBSCRIPTION=$SERVICE_SUBSCRIPTION"
    echo_verbose "  SERVICE_PREFIX=$SERVICE_PREFIX"
    echo_verbose "  SERVICE_NAME=$SERVICE_NAME"
    echo_verbose "  SERVICE_ENVIRONMENT=$SERVICE_ENVIRONMENT"
    echo_verbose "  SERVICE_INSTANCE=$SERVICE_INSTANCE"
    echo_verbose "  SERVICE_KEYVAULT_RESOURCE_GROUP=$SERVICE_KEYVAULT_RESOURCE_GROUP"
    echo_verbose "  SERVICE_KEYVAULT_NAME=$SERVICE_KEYVAULT_NAME"
    echo_verbose "  SERVICE_KEYVAULT_READER_OBJECTID=$SERVICE_KEYVAULT_READER_OBJECTID"
    echo_verbose "  SERVICE_KEYVAULT_READER_TENANTID=$SERVICE_KEYVAULT_READER_TENANTID"
}

set_environment_vars()
{
    echo_debug "$0::${FUNCNAME[0]}"
    export SERVICE_SUBSCRIPTION=$subscription_id
    export SERVICE_PREFIX=$prefix
    export SERVICE_NAME=$name
    export SERVICE_ENVIRONMENT=$env
    export SERVICE_INSTANCE=$instance
    export SERVICE_KEYVAULT_RESOURCE_GROUP=$keyvault_rg
    export SERVICE_KEYVAULT_NAME=$keyvault_name
    export SERVICE_KEYVAULT_READER_OBJECTID=$keyvault_reader_objectid 
    export SERVICE_KEYVAULT_READER_TENANTID=$keyvault_reader_tenantid
}

function pre_process_parameter_json()
{
    echo_debug "$0::${FUNCNAME[0]}"
    echo_verbose "Pre-processing ${f}"
    
    filename="$(basename "${f}")"
    input_filename="${f}"
    output_filename="$parameters_dir/$filename"
    if [ -f "${output_filename}" ]; then
        rm "${output_filename}"
    fi
    envsubst < $input_filename > $output_filename
    echo "${input_filename} --> ${output_filename}"
    echo_verbose "${output_filename}:\n$(cat "${output_filename}")"

    return 0
    # echo_error "${f}"
    # return 1
}

function main()
{
    echo_debug "$0::${FUNCNAME[0]}"
    set_environment_vars || return $?
    dump_environment_vars

    pushd "${parameters_templates_dir}" > /dev/null 
    last_error=0
    for f in *.parameters.json; do
        [ -f "${f}" ] || break
        pre_process_parameter_json "${f}"
        last_error=$?
        if [ ! ${last_error} -eq 0 ]; then
            break;
        fi 
    done
    popd > /dev/null
    return $last_error
}

main
