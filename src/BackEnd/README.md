# Backend Service for VS Online / Cloud Environments

## Overview

TODO

## Azure Batch Deployment Notes

A Batch Account and Pool are needed in each data-plane region and control-plane subscription:

BatchAccountName=vsodevcibatchusw2usw2, ResourceGroup=vsclk-online-dev-ci-usw2

BatchAccountName=vsopperelbatchusw2usw2, ResourceGroup=vsclk-online-ppe-rel-usw2
BatchAccountName=vsopperelbatchasseasse, ResourceGroup=vsclk-online-ppe-rel-asse

BatchAccountName=vsoprodrelbatchusw2usw2, ResourceGroup=vsclk-online-prod-rel-usw2
BatchAccountName=vsoprodrelbatchuseuse, ResourceGroup=vsclk-online-prod-rel-use
BatchAccountName=vsoprodrelbatcheuweuw, ResourceGroup=vsclk-online-prod-rel-euw
BatchAccountName=vsoprodrelbatchasseasse, ResourceGroup=vsclk-online-prod-rel-asse

The steps below will create a batch account and pool. Modify `BATCH_NAME`, `BATCH_RG`, `BATCH_REGION`, `BATCH_ENDPOINT` as appropriate.


```bash
BATCH_NAME=vsodevciusw2bausw2
BATCH_RG=vsclk-online-dev-ci-usw2
BATCH_REGION=westus2
BATCH_ENDPOINT=vsodevciusw2bausw2.westus2.batch.azure.com

# Create the batch account
az batch account create -l $BATCH_REGION -g $BATCH_RG -n $BATCH_NAME

BATCH_KEY=$(az batch account keys list -g $BATCH_RG -n $BATCH_NAME --query primary -otsv)

POOL_NODE_COUNT=2
POOL_NODE_SIZE=Standard_D4s_v3
# 4 cores on Standard_D4s_v3
POOL_CORES_ON_A_NODE=4
POOL_NODE_IMAGE=Canonical:ubuntuserver:18.04-lts:latest
POOL_NODE_SKU_ID="batch.node.ubuntu 18.04"

POOL_ID=storage-worker-pool
POOL_NODE_START_CMD='/bin/bash -cxe "printenv && wget -O azcopy.tar.gz https://azcopyvnext.azureedge.net/release20190703/azcopy_linux_amd64_10.2.1.tar.gz && tar -xf azcopy.tar.gz && mv azcopy_linux_amd64_*/azcopy $AZ_BATCH_NODE_SHARED_DIR/azcopy"'

# Create the pool
az batch pool create --account-endpoint $BATCH_ENDPOINT --account-name $BATCH_NAME --account-key $BATCH_KEY --id $POOL_ID --target-dedicated-nodes $POOL_NODE_COUNT --vm-size $POOL_NODE_SIZE --max-tasks-per-node $(($POOL_CORES_ON_A_NODE)) --image $POOL_NODE_IMAGE --node-agent-sku-id "$POOL_NODE_SKU_ID" --start-task-wait-for-success --start-task-command-line "$POOL_NODE_START_CMD"
```

### Creating a pool with App Insights Enabled

See https://docs.microsoft.com/en-us/rest/api/batchservice/pool/add#request-body for info on the structure of the json file.

```json
{
  "id": "debekoe-pool-9",
  "vmSize": "standard_d8s_v3",
  "virtualMachineConfiguration": {
    "imageReference": {
        "publisher": "canonical",
        "offer": "ubuntuserver",
        "sku": "18.04-lts",
        "version": "latest"
    },
    "nodeAgentSkuId": "batch.node.ubuntu 18.04",
    "dataDisks": [
        {
            "lun": 0,
            "caching": "ReadOnly",
            "diskSizeGB": 256,
            "storageAccountType": "Premium_LRS"
        }
    ]
  },
  "resizeTimeout": "PT15M",
  "targetDedicatedNodes": 2,
  "targetLowPriorityNodes": 0,
  "maxTasksPerNode": 8,
  "taskSchedulingPolicy": {
    "nodeFillType": "spread"
  },
  "enableAutoScale": false,
  "enableInterNodeCommunication": false,
  "startTask": {
      "commandLine": "/bin/bash -cxe \"printenv && wget  -O ai_run_linux.sh https://raw.githubusercontent.com/Azure/batch-insights/master/scripts/1.x/run-linux.sh && chmod +x ./ai_run_linux.sh && ./ai_run_linux.sh && wget -O azcopy.tar.gz https://azcopyvnext.azureedge.net/release20190703/azcopy_linux_amd64_10.2.1.tar.gz && tar -xf azcopy.tar.gz && mv azcopy_linux_amd64_*/azcopy $AZ_BATCH_NODE_SHARED_DIR/azcopy && DISK_NAME=`sfdisk -l | grep 256 -m 1| cut -d' ' -f2| cut -d':' -f1` && echo 'type=83' | sfdisk $DISK_NAME && echo -e 'y' |mkfs.ext4 $DISK_NAME && mkdir -p /datadrive && mount /dev/sdc /datadrive && mkdir -p /datadrive/images && chmod 777 /datadrive/images\"",
      "userIdentity": {
          "autoUser": {
              "scope": "Pool",
              "elevationLevel": "Admin"
          }
      },
      "environmentSettings": [
        {
          "name": "APP_INSIGHTS_INSTRUMENTATION_KEY",
          "value": "ENTER_VALUE_HERE"
        },
        {
          "name": "APP_INSIGHTS_APP_ID",
          "value": "b4d519b1-31a4-490e-a7e3-93992d1fd23f"
        },
        {
          "name": "BATCH_INSIGHTS_DOWNLOAD_URL",
          "value": "https://github.com/Azure/batch-insights/releases/download/v1.3.0/batch-insights"
        }
      ],
      "maxTaskRetryCount": 0,
      "waitForSuccess": true
  }
}
```

You can get the value of APP_INSIGHTS_INSTRUMENTATION_KEY from https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/86642df6-843e-4610-a956-fdd497102261/resourceGroups/vsclk-online-dev/providers/microsoft.insights/components/vsodevcibatchappinsights/overview.

```
BATCH_NAME=vsodevciusw2bausw2
BATCH_RG=vsclk-online-dev-ci-usw2
BATCH_REGION=westus2
BATCH_ENDPOINT=vsodevciusw2bausw2.westus2.batch.azure.com
BATCH_KEY=$(az batch account keys list -g $BATCH_RG -n $BATCH_NAME --query primary -otsv)
az batch pool create --account-endpoint $BATCH_ENDPOINT --account-name $BATCH_NAME --account-key $BATCH_KEY --json-file batch_config.json
```

## Testing

### Compute Provider Integration Tests
Create a `appsettings.test.json` like below in the directory of the tests:
```
{
  "CLIENT_ID": "b720128c-1a02-4cbe-8aa8-004cdf393123",
  "CLIENT_SECRET": "...",
  "TENANT_ID": "72f988bf-86f1-41af-91ab-2d7cd011db47"
}
```

### Storage Provider Integration Tests

Create a `appsettings.test.json` like below in the directory of the tests:
```
{
    "CLIENT_ID": "b720128c-1a02-4cbe-8aa8-004cdf393123",
    "CLIENT_SECRET": "...",
    "TENANT_ID": "72f988bf-86f1-41af-91ab-2d7cd011db47"
}
```

Get the client secret from here:

https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/asset/Microsoft_Azure_KeyVault/Secret/https://vsclk-online-dev-kv.vault.azure.net/secrets/app-sp-password/23fcb016764e460a854fb21f033ad492
