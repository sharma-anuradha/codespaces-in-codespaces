# Backend Service for VS Online / Cloud Environments

## Overview

TODO

## Testing

### Compute Provider Integration Tests

Switch to the dev subscription then create an SDK auth file using Azure CLI:
```
az account set --subscription 86642df6-843e-4610-a956-fdd497102261
az ad sp create-for-rbac --sdk-auth > .../bin/debug/ComputeVirtualMachineProvider.Test/azureauth.properties
```

### Storage Provider Integration Tests

Create a `appsettings.test.json` like below in the directory of the tests:
```
{
    "CLIENT_ID": "00000000-0000-0000-0000-000000000000",
    "CLIENT_SECRET": "00000000-0000-0000-0000-000000000000",
    "TENANT_ID": "72f988bf-86f1-41af-91ab-2d7cd011db47"
}
```

You can create a service principal with `az ad sp create-for-rbac --sdk-auth`.

