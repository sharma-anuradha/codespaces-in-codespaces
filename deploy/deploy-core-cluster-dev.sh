#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

deploy_cluster_cmd="./deploy-core-cluster.sh -v"

export SERVICE_ENV="dev"
export SERVICE_INSTANCE="ci"

# East US
export SERVICE_STAMP="eastus"
export SERVICE_STAMP_LOCATION="eastus"
${deploy_cluster_cmd} || exit $?

# West US 2
export SERVICE_STAMP="westus2"
export SERVICE_STAMP_LOCATION="westus2"
${deploy_cluster_cmd} || exit $?

# West Europe
export SERVICE_STAMP="westeurope"
export SERVICE_STAMP_LOCATION="westeurope"
${deploy_cluster_cmd} || exit $?

# Southeast Asia
export SERVICE_STAMP="southeastasia"
export SERVICE_STAMP_LOCATION="southeastasia"
${deploy_cluster_cmd} || exit $?
