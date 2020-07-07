# Batch testing and spinning up a dev pool
# Usage: ./batch_deploy_local_testing.sh validate
# Usage: ./batch_deploy_local_testing.sh create
# note: Run this script from the directory of this file.

deployment_operation=$1
if [ "${deployment_operation}" != "validate" ] && [ "${deployment_operation}" != "create" ]; then
  echo "Invalid deployment operation. Allowed values: 'validate', 'create'"
  exit 1
fi

# variables
subscription_id=$(az account show -s vsclk-core-dev --query id -otsv)
resource_group="vsclk-online-dev-ci-usw2"
template_file="../../arm/cluster-stamp-batch.json"
start_task_template_filepath="./start-commandline.sh"

# parameters
env="dev"
instance="ci"
stamp_name="usw2"
batch_account_prefix="vso"
region_code="usw2"
data_disk_gb=1024

gcs_environment="DiagnosticsProd"
gcs_account="VsOnlineDev"
gcs_namespace="VsOnlineDev"
gcs_role="vsonlinedevsecurity"
gcs_tenant="72f988bf-86f1-41af-91ab-2d7cd011db47"
gcs_pfx_base64=$(az keyvault secret show --subscription ${subscription_id} --vault-name vsclk-core-dev-kv --name vsclk-core-dev-monitoring --query value --output tsv)

start_task_tempfile=$(mktemp)
cat ${start_task_template_filepath} > ${start_task_tempfile}
sed -i -e "s/__REPLACE_GCS_ENVIRONMENT__/${gcs_environment}/g" ${start_task_tempfile}
sed -i -e "s/__REPLACE_GCS_ACCOUNT__/${gcs_account}/g" ${start_task_tempfile}
sed -i -e "s/__REPLACE_GCS_NAMESPACE__/${gcs_namespace}/g" ${start_task_tempfile}
sed -i -e "s/__REPLACE_GCS_ROLE__/${gcs_role}/g" ${start_task_tempfile}
sed -i -e "s/__REPLACE_GCS_TENANT__/${gcs_tenant}/g" ${start_task_tempfile}
sed -i -e "s;__REPLACE_GCS_PFX_BASE64__;${gcs_pfx_base64};g" ${start_task_tempfile}
batch_start_task_command_line_base64=$(cat ${start_task_tempfile} | (base64 --wrap=0 || base64) 2> /dev/null)
rm ${start_task_tempfile}
params=""
params="${params} --parameters env=${env} "
params="${params} --parameters instance=${instance} "
params="${params} --parameters stampName=${stamp_name} "
params="${params} --parameters accountNamePrefix=${batch_account_prefix} "
params="${params} --parameters accountNameRegionCode=${region_code} "
params="${params} --parameters dataDiskSizeInGb=${data_disk_gb}"
params="${params} --parameters startTaskCommandLineBase64=${batch_start_task_command_line_base64}"

# validate or create the deployment
az deployment group ${deployment_operation} --subscription ${subscription_id} --resource-group ${resource_group} --template-file ${template_file} ${params} --output json
