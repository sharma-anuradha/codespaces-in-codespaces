#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

deploy_cluster_cmd="./deploy-cluster.sh -v $@"

export SERVICE_SUBSCRIPTION="vsclk-core-ppe"
export SERVICE_NAME="online"
export SERVICE_ENV="ppe"
export SERVICE_INSTANCE="rel"
export SERVICE_DNS_NAME="online-ppe.core.vsengsaas.visualstudio.com"
export SSL_KV="vsclk-core-ppe-kv"
export SSL_SECRET="core-vsengsaas-visualstudio-com-ssl"

# Southeast Asia (deploy first to fail-fast on name-length overflows)
export SERVICE_STAMP="asse"
export SERVICE_STAMP_LOCATION="southeastasia"
${deploy_cluster_cmd} || exit $?

# East US
export SERVICE_STAMP="use"
export SERVICE_STAMP_LOCATION="eastus"
${deploy_cluster_cmd} || exit $?

# West US 2
export SERVICE_STAMP="usw2"
export SERVICE_STAMP_LOCATION="westus2"
${deploy_cluster_cmd} || exit $?

# West Europe
export SERVICE_STAMP="euw"
export SERVICE_STAMP_LOCATION="westeurope"
${deploy_cluster_cmd} || exit $?
