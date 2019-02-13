#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Deploy cluster vssvc-sample-dev-johnri in eastus
subscription="4f2b6795-3bd7-4414-ac3d-13649ef1e336"
#cluster_only="--cluster-only"
cluster_only=""
#validate="--validate"
validate=""
prefix="vssvc"
servicename="sample"
env="dev"
instance="johnri"
location="eastus"
./deploy-cluster.sh -d -v $validate $cluster_only \
    --subscription $subscription \
    --prefix $prefix \
    --servicename $servicename \
    --env $env \
    --instance $instance \
    --location $location
