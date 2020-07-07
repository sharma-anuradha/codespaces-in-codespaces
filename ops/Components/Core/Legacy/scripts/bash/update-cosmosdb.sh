#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

function update_cosmosdb()
{
    env="$(set +u; echo "${1}")"
    instance_override="$(set +u; echo "${2}")"

    if [ -z "${env}" ]; then
        echo "ERROR: environment is required" 1>&2
        return 1
    fi

    location_eastus="eastus=0"
    location_westus2="westus2=1"
    location_westeurope="westeurope=2"
    location_southeastasia="southeastasia=3"
    instance=
    locations=

    case "$env" in
        dev)
            instance="ci"
            locations="${location_eastus} ${location_westus2}"
            ;;
        ppe)
            instance="rel"
            locations="${location_eastus} ${location_westus2} ${location_westeurope} ${location_southeastasia}"
            ;;
        prod)
            instance="rel"
            locations="${location_eastus} ${location_westus2} ${location_westeurope} ${location_southeastasia}"
            ;;
        *)
            echo "ERROR: invalid env: ${env}" 1>&2
            echo "Valid environments are dev, ppe, prod." 1>&2
            return 1
    esac

    if [ ! -z "${instance_override}" ]; then
        instance="${instance_override}"
    fi

    subscription="vsclk-core-${env}"
    resource_group="vsclk-online-${env}-${instance}"
    database_name="${resource_group}-db"

    echo "Processing /subscriptions/${subscription}/resourceGroups/${resource_group}/database/${database_name}"

    az_command="az cosmosdb update --subscription ${subscription} -g ${resource_group} -n ${database_name} --default-consistency-level BoundedStaleness --max-interval 300 --max-staleness-prefix 100000"
    echo "${az_command}"
    ${az_command}

    az_command="az cosmosdb update --subscription ${subscription} -g ${resource_group} -n ${database_name} --enable-multiple-write-locations --locations ${locations}"
    echo "${az_command}"
    ${az_command}

    echo

    return 0
}

# update_cosmosdb dev stg
update_cosmosdb dev ci
# update_cosmosdb ppe load
# update_cosmosdb ppe rel
# update_cosmosdb prod rel
