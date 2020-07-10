# Azure Subscriptions

This document lists the set of azure subscriptions used to run the [Visual Studio Codespaces Service](https://servicetree.msftcloudes.com/main.html#/ServiceModel/Home/8fa58105-2fc7-4ffb-8d9e-5654c301864b).

- [Subscription Provisioning Plan](#subscription-provisioning-plan)
- [Tenants and Access Control](#tenants-and-access-control)
- [List of Supported Regions](#list-of-supported-regions)
- [List of Subscrptions](#list-of-subscriptions)

## Subscription Provisioning Plan

Azure subscriptions are provisioned per component, per environment, and per plane. The Ops and Control plane subscriptions will serve all regions.

Data plane subscriptions are also provisioned per region and serve a single region, and may contain a resource type category to help manage capacity, for example, -compute or -storage. Data-plane subscriptions should have an ordinal -000, -001, etc., to anticipate future scale-out.

If a component does do not require its own Ops, Control, or Data subscription, none should be provisioned.

## Tenants and Access Control

Non-production subscriptions, in the dev and test environments, are provisioned in the CORP tenant. Team members will have direct access, either Owner or Contributor roles, to the subscriptions and to Key Vaults. Non-production subscriptions do not require JIT access.Access will be granted via groups defined via [MyAccess](https://aka.ms/myaccess).

- *TBD: list default role assignments per group.*
- *TBD: list default keyvault access policies per group.*

Productions subscriptions, in the ppe and prod environments, are provisioned in the PME tenant. Team members will have no access by default--not even Reader. The subscriptions will be listed in the portal when signing in with a PME identity. These subscriptions require JIT access via groups defined via [OneIdentity](https://aka.ms/oneidentity).

- *TBD: list default role assignments per group.*
- *TBD: list default keyvault access policies per group.*
- *TBD: How to create empty groups that you have to JIT into specifically? This is to limit JIT access.*

## How to Provision and Onboard

New subscriptions must be provisioned via [AIRS](https://aka.ms/airs) using the appropriate [approved registration](https://azuremsregistration.microsoft.com/MyApprovedRegistrations.aspx).

### Provisioning Non-Production Subscriptions

- Select the AIRs registration that corresponds to your **@microsoft.com** identity.
- Click the "Add Subscription" button and sign in with your **@microsoft.com** identity.
- Select service [Visual Studio Codespaces](https://servicetree.msftcloudes.com/main.html#/ServiceModel/Home/8fa58105-2fc7-4ffb-8d9e-5654c301864b)
- Use PCCode `P74841`.

### Provisioning Production Subscriptions

- Select the AIRs registration that corresponds to your **@pme.gbl.msidentity.com** identity.
- Click the "Add Subscription" button and sign in with your **@pme.gbl.msidentity.com** identity.
- Select service [Visual Studio Codespaces](https://servicetree.msftcloudes.com/main.html#/ServiceModel/Home/8fa58105-2fc7-4ffb-8d9e-5654c301864b)
- Use PCCode `P10168064`.

### Onboarding a New Subscription

After the subscription has been created, onboarded the subscription by following these steps.

1. Update the [List of Subscrptions](#list-of-subscriptions) below. This will help your fellow engineers to find subscription IDs for JIT requests, since the PPE and PROD subscriptions are not visible by default in the portal.
1. *TBD: Run the relevant ARM Subscription templates. This will set appropriate RBAC and ownership.*
1. *TBD: Set the subscription administrator to an ALT_breakglass account.*
1. *TBD: Add the subscription to the relevant appsettings.*.json file.*

## List of Supported Regions

Data plane subscriptions must be provisioned for all supported regions.

| Geo | Region | Region Code |
| --- | --- | --- |
| Asia-Pacific | Southeast Asia | `ap-se` |
| Australia | Australia East | `au-e` |
| Europe | West Europe | `eu-w` |
| United Kingdom | UK South | `uk-s` |
| United States | East US | `us-e` |
| United States | West US 2 | `us-w2` |

## List of Subscriptions

| Name | ID | Tenant | Component | Environment | Plane | Usage |
| --- | --- | --- | --- | --- | --- | --- |
| [vscs-codesp-dev-ctl ]() | `TBD` | CORP | Codespaces | Dev | Control | Codespaces control, all regions |
| [vscs-codesp-dev-ctl-github]() | `TBD` | CORP | Codespaces | Dev | Control | GitHub partner integration |
| [vscs-codesp-dev-data-compute-ap-se-000]() | `TBD` | CORP | Codespaces | Dev | Data | Compute resources |
| [vscs-codesp-dev-data-compute-au-e-000]() | `TBD` | CORP | Codespaces | Dev | Data | Compute resources |
| [vscs-codesp-dev-data-compute-eu-w-000]() | `TBD` | CORP | Codespaces | Dev | Data | Compute resources |
| [vscs-codesp-dev-data-compute-uk-s-000]() | `TBD` | CORP | Codespaces | Dev | Data | Compute resources |
| [vscs-codesp-dev-data-compute-us-e-000]() | `TBD` | CORP | Codespaces | Dev | Data | Compute resources |
| [vscs-codesp-dev-data-compute-us-w2-000]() | `TBD` | CORP | Codespaces | Dev | Data | Compute resources |
| [vscs-codesp-dev-data-images-ap-se-000]() | `TBD` | CORP | Codespaces | Dev | Data | Image resources |
| [vscs-codesp-dev-data-images-au-e-000]() | `TBD` | CORP | Codespaces | Dev | Data | Image resources |
| [vscs-codesp-dev-data-images-eu-n-000]() | `TBD` | CORP | Codespaces | Dev | Data | Image resources |
| [vscs-codesp-dev-data-images-uk-s-000]() | `TBD` | CORP | Codespaces | Dev | Data | Image resources |
| [vscs-codesp-dev-data-images-us-e-000]() | `TBD` | CORP | Codespaces | Dev | Data | Image resources |
| [vscs-codesp-dev-data-images-us-w2-000]() | `TBD` | CORP | Codespaces | Dev | Data | Image resources |
| [vscs-codesp-dev-data-storage-ap-se-000]() | `TBD` | CORP | Codespaces | Dev | Data | Storage resources |
| [vscs-codesp-dev-data-storage-au-e-000]() | `TBD` | CORP | Codespaces | Dev | Data | Storage resources |
| [vscs-codesp-dev-data-storage-eu-w-000]() | `TBD` | CORP | Codespaces | Dev | Data | Storage resources |
| [vscs-codesp-dev-data-storage-uk-s-000]() | `TBD` | CORP | Codespaces | Dev | Data | Storage resources |
| [vscs-codesp-dev-data-storage-us-e-000]() | `TBD` | CORP | Codespaces | Dev | Data | Storage resources |
| [vscs-codesp-dev-data-storage-us-w2-000]() | `TBD` | CORP | Codespaces | Dev | Data | Storage resources |
| [vscs-collab-dev-ctl ]() | `TBD` | CORP | Collaboration | Dev | Control | "Live Share" control, all regions |
| [vscs-collab-dev-data-ap-se-000]() | `TBD` | CORP | Collaboration | Dev | Data | Relays |
| [vscs-collab-dev-data-au-e-000]() | `TBD` | CORP | Collaboration | Dev | Data | Relays |
| [vscs-collab-dev-data-eu-w-000]() | `TBD` | CORP | Collaboration | Dev | Data | Relays |
| [vscs-collab-dev-data-uk-s-000]() | `TBD` | CORP | Collaboration | Dev | Data | Relays |
| [vscs-collab-dev-data-us-e-000]() | `TBD` | CORP | Collaboration | Dev | Data | Relays |
| [vscs-collab-dev-data-us-w2-000]() | `TBD` | CORP | Collaboration | Dev | Data | Relays |
| [vscs-comm-dev-ctl ]() | `TBD` | CORP | Communications | Dev | Control | Communications control (Empty?), all regions |
| [vscs-comm-dev-data-ap-se-000]() | `TBD` | CORP | Communications | Dev | Data | SignalR |
| [vscs-comm-dev-data-au-e-000]() | `TBD` | CORP | Communications | Dev | Data | SignalR |
| [vscs-comm-dev-data-eu-w-000]() | `TBD` | CORP | Communications | Dev | Data | SignalR |
| [vscs-comm-dev-data-uk-s-000]() | `TBD` | CORP | Communications | Dev | Data | SignalR |
| [vscs-comm-dev-data-us-e-000]() | `TBD` | CORP | Communications | Dev | Data | SignalR |
| [vscs-comm-dev-data-us-w2-000]() | `TBD` | CORP | Communications | Dev | Data | SignalR |
| [vscs-core-dev-ctl ]() | `TBD` | CORP | Core | Dev | Control | Shared Core Cluster |
| [vscs-core-dev-ops ]() | `TBD` | CORP | Core | Dev | DevOps | Shared DevOps |
| [vscs-core-dev-ops-monitoring]() | `TBD` | CORP | Core | Dev | DevOps | Geneva CodespacesDev |
| [vscs-core-test](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/ebf675df-782e-47f7-a68d-b4c4696a28e2/overview) | `ebf675df-782e-47f7-a68d-b4c4696a28e2` | CORP | Core | Test | (any) | Development R & D |
