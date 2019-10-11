# Onboarding Data-Plane Subscriptions

Azure Resources that are hosted on-behalf of customers live in data-plane subscriptions. These subscriptions are distinct from the control-plane, where our front-end and back-end services run.

Resource capacity for the data-plane subscriptions is defined in [VSO Subscription Capacity](https://microsoft.sharepoint.com/teams/VSSaaS/Shared%20Documents/Cloud%20Workspaces/VSO%20Subscription%20Capacity%20Modeler.xlsx?web=1).

Capacity is added in chuncks referred as a "scale unit group". Each group has capacity for 1000 environment SKUs per region, or 4000 environments per subscription. This requires 4 subscriptions due to fixed limitations on storage accounts and virtual networks.

This document explains how to onboard one subscription at a time.

- [Provisioining New Data-Plane Subscriptions](#provisioining-new-data-plane-subscriptions)
- [Configuring Data-Plane Subscriptions](#configuring-data-plane-subscriptions)
- [Service Configuration and Deployment](#service-configuration-and-deployment)

## Provisioining New Data-Plane Subscriptions

New data-plane subscriptions can be requested from maggiexu@microsoft.com or self-service from [AIRS](https://aka.ms/airs).

| Property | Value |
| --- | --- |
| Name (general) | vso-prod-data-{group}-{orginal} |
| Service Tree | [8fa58105-2fc7-4ffb-8d9e-5654c301864b](https://servicetree.msftcloudes.com/main.html#/ServiceModel/Home/8fa58105-2fc7-4ffb-8d9e-5654c301864b) |
| Subscription Admin | johnri@microsoft.com (or the requestor) |
| Environment | production |
| PCCode | P10168064 Visual Studio COGs Lic |
| Estimated Cost† | $5600/month max for storage only |
| Estimated End Date | none, long-term production |

Where `group` is the scale unit group number, starting with 1. At the moment, production requires 8 groups for a total capaciy of 8 x 4000 = 32000 SKUs. `Ordinal` is the sub-number within the group, typically in the range [0..3]. The capacity planning document suggests that groups 100+ are dedicated to additional storage capacity, with ordinal numbering [1..*].

† This value is based on storage only, if all possible storage accounts were consumed. See the [COGs spreadsheet](https://microsoft.sharepoint.com/teams/VSSaaS/Shared%20Documents/Cloud%20Workspaces/Cloud%20Environments%20COGs.xlsx?web=1) for more data. The number can varry wildly depending on pool sizes and number of active SKUs.

## Configuring Data-Plane Subscriptions

- [Update Service Tree metadata](#update-service-tree-metadata)
- [Update access control](#update-access-control)
- [Request quota increases](#request-quota-increases)
- [Update subscription owner](#update-subscription-owner)

### Update Service Tree Metadata

The subscription metadata in [Service Tree](https://servicetree.msftcloudes.com/main.html#/ServiceModel/Service/Subscription/8fa58105-2fc7-4ffb-8d9e-5654c301864b) should be modifed as follows

- Set "Hosted On-Behalf of Customers" to `true`.
- Leave "HOBO beneficiary service is internal" as `false`.

### Update Access Control

Either in the Azure Portal or via the command line, the subscription admin must add the following Role Assignments.

| Scope | Assignee | Role |
| --- | --- | --- |
| Subscription | vsclk-online-prod-app-sp | Contributor |
| Subscription | vsclk-core-breakglass-823b | Owner |
| Subscription | vsclk-core-readers-fd84 | Reader |

```bash
subscription="???"
prod_app_sp="0343473b-5b3b-45ff-96a4-42afab47ef14" # vsclk-online-prod-app-sp
breakglass_group="86701306-e3cd-4b17-85a1-2956e25a2527" # vsclk-core-breakglass-823b
readers_group="6837c2b1-4f15-45e3-a9f5-9bfac0726a47" # vsclk-core-readers-fd84
az role assignment create --subscription ${subscription} --scope /subscriptions/${subscription} --assignee ${prod_app_sp} --role Contributor
az role assignment create --subscription ${subscription} --scope /subscriptions/${subscription} --assignee ${breakglass_group} --role Owner
az role assignment create --subscription ${subscription} --scope /subscriptions/${subscription} --assignee ${readers_group} --role Reader
```

### Request Quota Increases

The default capacity of 250 storage accounts per region and 1000 virtual networks per region is assumed. These need not be increased.

If additional cores are required, the subscription owner must request increases according to the desired capacity using the Azure Portal. Here is an example of what _might_ be requested, not what must be requested.

| Service | Quota | Location | Limit |
| --- | --- | --- | --- |
| Compute | FSv2 Cores | (Asia Pacific) Southeast Asia | 4800 |
| Compute | FSv2 Cores | (Europe) West Europe | 4800 |
| Compute | FSv2 Cores | (US) East US | 4800 |
| Compute | FSv2 Cores | (US) West US 2 | 4800 |


### Update Subscription Owner

After the above RBAC and quota increases have been completed, the subscription owner should be modified from a personal account, such as johnri@microsoft.com, to a break-glass account. We typically use SC-jr247@microsoft.com.

## Service Configuration and Deployment

Brining the new subscription capacity onboard requires changes to AppSettings and a re-deployment. The configuration changes should go through a normal Pull Request review and be deployed via standard processes.

Production subscriptions must be added to the file `appsettings.prod-rel.json` as follows:

```json
{
  "AppSettings": {
    "dataPlaneSettings": {
      "subscriptions": {
        "{{ subscriptionName }}": {
          "subscriptionId": "{{ subscriptionId }}",
          "locations": [
            "EastUs",
            "SoutheastAsia",
            "WestEurope",
            "WestUs2"
          ],
          "quotas": {
            "compute": {
              "cores": 4800,
              "standardFSv2Family": 4800
            },
            "storage": {
              "StorageAccounts": 250
            },
            "network": {
              "VirtualNetworks": 1000
            }
          }
        }
      }
    }
  }
}
```

The `quotas` element can be omitted if the defaults are appropriate. For a storage-only subscription, `quotas.compute` and `quotas.network` would be omitted.

