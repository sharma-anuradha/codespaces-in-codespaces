# DEPLOY STEPS
# create a resource group first fro example:
az group create --name vsls-presence-servicer-group --location "Central US"

az group deployment create --name vsls-presence-servicer-deployment --resource-group YOUR_GROUP --template-file azuredeploy.json --parameters azuredeploy.parameters.dev.json --parameters dockerRegistryPassword=YOUR_PSWD

