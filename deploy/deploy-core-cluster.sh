#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

deploy_core_cluster_script_name="$( basename "${BASH_SOURCE}" )"
deploy_core_cluster_script_dir="$( cd "$( dirname "${BASH_SOURCE}" )" >/dev/null 2>&1 && pwd )"
cluster_scripts_dir="${deploy_core_cluster_script_dir}/cluster/src/scripts/bash"

# This overrides the cluster subdirectory for testing changes in the 
# external repo at the same time
args="$@"
if [ ! -z ${1+x} ]; then
    if [ "${1}" == "--local" ]; then
        shift        
        cluster_scripts_dir="${1}"
        args="${args#--local ${cluster_scripts_dir}}"
        script_name="$( basename "${BASH_SOURCE}" )"
        echo "${deploy_core_cluster_script_name} using local cluster script dir '${cluster_scripts_dir}' with args '${args}'"
    fi
fi

# Utilities
. "${cluster_scripts_dir}/utilities.sh"

# Set up deployment parameters and defaults
default_location="eastus"

subscription=$(set +u; [ ! -z $SERVICE_SUBSCRIPTION ] && echo $SERVICE_SUBSCRIPTION || echo "vsclk-core-dev")
prefix=$(set +u; [ ! -z $SERVICE_PREFIX ] && echo $SERVICE_PREFIX || echo "vsclk")
name=$(set +u; [ ! -z $SERVICE_NAME ] && echo $SERVICE_NAME || echo "core")
env=$(set +u; [ ! -z $SERVICE_ENV ] && echo $SERVICE_ENV || echo "dev")
instance=$(set +u; [ ! -z $SERVICE_INSTANCE ] && echo $SERVICE_INSTANCE || echo "ci")
stamp=$(set +u; [ ! -z $SERVICE_STAMP ] && echo $SERVICE_STAMP || echo "${default_location}")
stamp_location=$(set +u; [ ! -z $SERVICE_STAMP_LOCATION ] && echo $SERVICE_STAMP_LOCATION || echo "${default_location}")
location=$(set +u; [ ! -z $SERVICE_LOCATION ] && echo $SERVICE_LOCATION || echo "${default_location}")
team_group_name=$(set +u; [ ! -z $SERVICE_TEAM_GROUP_NAME ] && echo $SERVICE_TEAM_GROUP_NAME || echo "vsclk-core-contributors-3a5d")

# Other command line options that can be passed
#      --generate-only generate ARM parameters only
#      --cluster-only  deploy only the cluster, not the environment or instance
#      --validate_only      validate_only deployment templates, do not deploy
#      --dry-run       do not create or deploye Azure resources
#  -h, --help          show help
#  -v, --verbose       emit verbose info
#  -d, --debug         emit debug info

echo_info "Deploying cluster $prefix-$name-$env-$instance-$stamp into subscription $subscription"

. "${cluster_scripts_dir}/deploy-cluster.sh" \
    --subscription $subscription \
    --prefix $prefix \
    --name $name \
    --env $env \
    --instance $instance \
    --stamp $stamp \
    --stamp-location $stamp_location \
    --location $location \
    --team-group-name $team_group_name \
    ${args}
