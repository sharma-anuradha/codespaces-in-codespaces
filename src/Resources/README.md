# Backend Service for VS Online / Cloud Environments

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

https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/asset/Microsoft_Azure_KeyVault/Secret/https://vsclk-online-dev-kv.vault.azure.net/secrets/app-sp-password
