#!/bin/bash

# Other command line options that can be passed
#      --generate-only generate ARM parameters only
#      --cluster-only  deploy only the cluster, not the environment or instance
#      --validate_only      validate_only deployment templates, do not deploy
#      --dry-run       do not create or deploye Azure resources
#  -h, --help          show help
#  -v, --verbose       emit verbose info
#  -d, --debug         emit debug info
args="$@"

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset
deploy_core_cluster_script_name=$( basename "${BASH_SOURCE}" )
deploy_core_cluster_script_dir=$( cd "$( dirname "${BASH_SOURCE}" )" >/dev/null 2>&1 && pwd )

# initialize the vsclk-cluster location
git_root="$(git rev-parse --show-toplevel)"
echo "${git_root}"
vsclk_cluster_dir="$(set +u; [ ! -z $VSCLK_CLUSTER_DIR ] && echo $VSCLK_CLUSTER_DIR || echo "${git_root}/../vsclk-cluster")"
if [ ! -d "${vsclk_cluster_dir}" ]; then
    echo "Cluster directory not found: '${vsclk_cluster_dir}'"
    exit 1
fi
vsclk_cluster_dir="$( cd "${vsclk_cluster_dir}" >/dev/null 2>&1 && pwd )"
cluster_scripts_dir="$( cd "${vsclk_cluster_dir}/src/scripts/bash" >/dev/null 2>&1 && pwd )"

# Utilities
. "${cluster_scripts_dir}/utilities.sh"

# Set up deployment parameters and defaults for vsclk-core
default_subscription="vsclk-core-dev"
default_prefix="vsclk"
default_name=""         ## NO DEFAULT
default_env="dev"
default_instance="ci"
default_location="eastus"
default_stamp="use"
default_stamp_location="${default_location}"
default_team_group_name="vsclk-core-contributors-3a5d"
default_ssl_kv=""       ## NO DEFAULT 
default_ssl_secret=""   ## NO DEFAULT
default_dns_name=""     ## NO DEFAULT

subscription=$(set +u; [ ! -z $SERVICE_SUBSCRIPTION ] && echo $SERVICE_SUBSCRIPTION || echo "${default_subscription}")
prefix=$(set +u; [ ! -z $SERVICE_PREFIX ] && echo $SERVICE_PREFIX || echo "${default_prefix}")
name=$(set +u; [ ! -z $SERVICE_NAME ] && echo $SERVICE_NAME || echo "${default_name}")
env=$(set +u; [ ! -z $SERVICE_ENV ] && echo $SERVICE_ENV || echo "${default_env}")
instance=$(set +u; [ ! -z $SERVICE_INSTANCE ] && echo $SERVICE_INSTANCE || echo "${default_instance}")
stamp=$(set +u; [ ! -z $SERVICE_STAMP ] && echo $SERVICE_STAMP || echo "${default_stamp}")
stamp_location=$(set +u; [ ! -z $SERVICE_STAMP_LOCATION ] && echo $SERVICE_STAMP_LOCATION || echo "${default_stamp_location}")
location=$(set +u; [ ! -z $SERVICE_LOCATION ] && echo $SERVICE_LOCATION || echo "${default_location}")
team_group_name=$(set +u; [ ! -z $SERVICE_TEAM_GROUP_NAME ] && echo $SERVICE_TEAM_GROUP_NAME || echo "${default_team_group_name}")
ssl_kv=$(set +u; [ ! -z $SSL_KV ] && echo $SSL_KV || echo "${default_ssl_kv}")
ssl_secret=$(set +u; [ ! -z $SSL_SECRET ] && echo $SSL_SECRET || echo "${default_ssl_secret}")
dns_name=$(set +u; [ ! -z $SERVICE_DNS_NAME ] && echo $SERVICE_DNS_NAME || echo "${default_dns_name}")

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
    --ssl-cert-kv-name $ssl_kv \
    --ssl-cert-secret-name $ssl_secret \
    --dns-name $dns_name \
    ${args}
