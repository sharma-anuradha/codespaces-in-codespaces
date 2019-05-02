#!/bin/bash


# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset
args="$@"

function required() {
    local name="$1"
    local value="$(set +u; echo "$2")"

    if [ -z "$value" ]; then
        echo "$name is required"
        return 1
    fi

    echo "$value"
    return 0
}

function az_command() {
    local command="az $@ --subscription $subscription"
    $command
}

function update_secret() {
    local secret_name="$1"
    local secret_value="$2"
    az_command keyvault secret set --vault-name $vault_name --name $secret_name --value $secret_value
}

function get_db_auth_key() {
    local connection_string="$(az_command cosmosdb list-connection-strings -g $env_rg -n $cosmosdb_id --query "connectionStrings[0].connectionString" -o tsv)" || return $?
    local accountkey="$(echo $connection_string | cut -d';' -f2)"
    local auth_key="$(echo $accountkey | cut -d'=' -f2)"
    echo $auth_key
}

function get_redis_connection_string() {
    local primary_key="$(az_command redis list-keys -g $env_rg -n $redis_id --query "primaryKey" -o tsv)" || return $?
    local connection_string="${redis_id}.redis.cache.windows.net:6380,password=${primary_key},ssl=True,abortConnect=False"
    echo $connection_string
}

function get_signalr_connection_string {
    az_command signalr key list -g $stamp_rg -n $signalr_id --query "primaryConnectionString" -o tsv || return $?
}

# Config-AzureCosmosDbAuthKey
# Config-AzureCosmosDbHost
# Config-MicrosoftAppClientSecret
# Config-RedisConnectionString
# Config-SignalRConnectionString

function main() {
    echo "${subscription}"
    echo "${vault_name}"
    echo "${cosmosdb_id}"
    echo "${redis_id}"
    echo "${signalr_id}"

    local db_auth_key="$(get_db_auth_key)" || return $?
    local redis_connection_string="$(get_redis_connection_string)" || return $?
    local signalr_connection_string="$(get_signalr_connection_string)" || return $?

    update_secret "Config-AzureCosmosDbAuthKey" $db_auth_key || return $?
    update_secret "Config-RedisConnectionString" $redis_connection_string || return $?
    update_secret "Config-SignalRConnectionString" $signalr_connection_string || return $?
    echo "Success"
}

# Globals
subscription="$(set +u; required "service_name" "$1")"
service_name="$(set +u; required "service_name" "$2")" || (echo $service_name && exit 1)
env="$(set +u; required "env" "$3")" || (echo $env && exit 1)
instance="$(set +u; required "instance" "$4")" || (echo $instance && exit 1)

prefix="vsclk"
env_rg="${prefix}-${service_name}-${env}"
vault_name="${env_rg}-kv"
cosmosdb_id="${env_rg}-db"
redis_id="${env_rg}-redis"
stamp="use"
stamp_rg="${env_rg}-${instance}-${stamp}"
signalr_id="${stamp_rg}-signalr"

main