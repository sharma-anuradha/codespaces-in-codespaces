# Batch testing and spinning up a dev service bus
# note: Run this script from the directory of this README.

deployment_operation=$1
if [ "${deployment_operation}" != "validate" ] && [ "${deployment_operation}" != "create" ]; then
  echo "Invalid deployment operation. Allowed values: 'validate', 'create'"
  exit 1
fi

# variables
subscription_id=$(az account show -s vsclk-core-dev --query id -otsv)
resource_group="vsclk-online-dev-ci-usw2"
template_file="../../arm/cluster-stamp-service-bus.json"

# parameters
resource_name_prefix="vsclk"
service_name="online"
env="dev"
instance="ci"
stamp_name="usw2"

params=""
params="${params} --parameters resourceNamePrefix=${resource_name_prefix} "
params="${params} --parameters serviceName=${service_name} "
params="${params} --parameters env=${env} "
params="${params} --parameters instance=${instance} "
params="${params} --parameters stampName=${stamp_name} "

# validate or create the deployment
az deployment group ${deployment_operation} --subscription ${subscription_id} --resource-group ${resource_group} --template-file ${template_file} ${params} --output json
