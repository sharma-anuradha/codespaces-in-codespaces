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

debug=0
verbose=1
subscription="4f2b6795-3bd7-4414-ac3d-13649ef1e336"
prefix="vssvc"
servicename="sample"
env="dev"
instance="johnri"
location="eastus"

# Emit verbose output for parameters
echo_verbose "Parameters: "
echo_verbose "  debug: ${debug}"
echo_verbose "  verbose: ${verbose}"
echo_verbose "  prefix: $prefix"
echo_verbose "  servicename: $servicename"
echo_verbose "  env: $env"
echo_verbose "  instance: $instance"
echo_verbose "  location: $location"

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

function delete_resource_group()
{
    local RG="${1}"
    local STATE=$(az group show -n $RG --query properties.provisioningState -o tsv 2>/dev/null)
    if [[ $STATE == "Succeeded" ]] ; then
        echo_verbose "Deleting resource group $RG..."
        az group delete -n $RG --yes >/dev/null || return $?
    fi
    return 0
}

function delete_environment_resource_group()
{
    delete_resource_group $environment_rg
}

function delete_instance_resource_group()
{
    delete_resource_group $instance_rg $location
}

delete_cluster_sp()
{
    if [[ $(az ad sp show --id "http://$cluster_sp_name" -o json) ]] ; then
        echo_verbose "Deleting sp $cluster_sp_name"
        az_command="az ad sp delete --id http://$cluster_sp_name"
        echo_verbose "$az_command"
        $az_command
    fi
    return 0
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

# Script Variables
environment_name="$prefix-$servicename-$env"
instance_name="$prefix-$servicename-$env-$instance"
environment_rg="$environment_name"
instance_rg="$instance_name"
cluster_sp_name="$environment_name-cluster-sp"
echo_verbose "Script variables:"
echo_verbose "  cluster_sp_name: $cluster_sp_name"
echo_verbose "  environment_name: $environment_name"
echo_verbose "  environment_rg: $environment_rg"
echo_verbose "  instance_name: $instance_name"
echo_verbose "  instance_rg: $instance_rg"

function main()
{
    set_az_subscription || return $?
    delete_cluster_sp || return $?
    delete_environment_resource_group || return $?
    delete_instance_resource_group || return $?
}

main
