# Application Registrations, Service Principals, and Service Connections

- [First-party application registrations](#first-party-application-registrations) are required for a number of high-security identity concerns, specifically when representing the Codespaces service public for service-to-service scenarios.
- [DevOps Service Princiapls](#devops-service-principals) have Contributor access to the service Ops and Control planes for service deployment and configuration. These typically have one ore more associated Azure DevOps service connections. DevOps service principals _should not_ have access to the Data plane, to guard customer data.
- [Legacy Multi-Tenant Service Princiapls](#legacy-multi-tenant-service-principals) __ have Contributor access to legacy legacy Control and Data plane subscriptoins.
- [Managed Identies](#managed-identities) have Contributor access from the runtime service to the service Control and Data planes.

## First-Party Application Registrations

## DevOps Service Principals

**TODO: Which service principals can be replaced by Managed Identity / POD Identity.**

- If Ops service principals can access Ops and Control plane, but cannot obtain Managed Identity secrets, then leaking the Ops SP cannot give up customer data!
- If Control service principals are replaced with Managed Identity, then the Control Plane can access Control and Data, but a service principal cert isn't required to access customer data.

| Name | App ID | Tenant | Certificate Key Vault | Component | Environment |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [vscs-codesp-dev-ops-sp ]() | `TBD` | CORP | vscs-codesp-dev-ops-kv | Codespaces | Dev |
| [vscs-codesp-ppe-ops-sp ]() | `TBD` | PME | vscs-codesp-ppe-ops-kv | Codespaces | PPE |
| [vscs-codesp-prod-ops-sp ]() | `TBD` | PME | vscs-codesp-prod-ops-kv | Codespaces | Prod |
| [vscs-core-dev-ops-sp ]() | `TBD` | CORP | vscs-core-dev-ops-kv | Core | Dev |
| [vscs-core-ppe-ops-sp ]() | `TBD` | PME | vscs-core-ppe-ops-kv | Core | PPE |
| [vscs-core-prod-ops-sp ]() | `TBD` | PME | vscs-core-prod-ops-kv | Core | Prod |
| [vscs-collab-dev-ops-sp ]() | `TBD` | CORP | vscs-collab-dev-ops-kv | Collaboration | Dev |
| [vscs-collab-ppe-ops-sp ]() | `TBD` | PME | vscs-collab-ppe-ops-kv | Collaboration | PPE |
| [vscs-collab-prod-ops-sp ]() | `TBD` | PME | vscs-collab-prod-ops-kv | Collaboration | Prod |

### Azure DevOps Service Connections

See [Deploy to *ME Subscriptions from ADO](https://dev.azure.com/msazure/AzureWiki/_wiki/wikis/AzureWiki.wiki/33392/Deploy-to-ME-Subscriptions-from-ADO).

For an Azure Resource Manager service connection:

- Environment: Azure Cloud
- Scope Level: Subscription
- Subscription ID
- Client ID: the service principal Application ID
- Certificate: **ON A SAW machine**, download the certificate as a .pem file; copy and paste the certificate data; delete the .pem file. This will require JIT for PPE and Prod.


## Legacy Multi-Tenant Service Principals

The new service builout requires access to some legacy subscriptions that will remain in the CORP tenant. Managed Identity is not supported for this cross-tenant scenario. The service will be configured to use the service principals for accessing specific subscriptions.

| Name | App ID | Tenant | Certificate Key Vault | Component | Environment |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [vscs-codesp-dev-ctl-sp ]() | `TBD` | CORP | vscs-codesp-dev-ctl-kv | Codespaces | Dev |
| [vscs-codesp-ppe-ctl-sp ]() | `TBD` | PME | vscs-codesp-ppe-ctl-kv | Codespaces | PPE |
| [vscs-codesp-prod-ctl-sp ]() | `TBD` | PME | vscs-codesp-prod-ctl-kv | Codespaces | Prod |
| [vscs-collab-dev-ctl-sp ]() | `TBD` | CORP | vscs-collab-dev-ctl-kv | Collaboration | Dev |
| [vscs-collab-ppe-ctl-sp ]() | `TBD` | PME | vscs-collab-ppe-ctl-kv | Collaboration | PPE |
| [vscs-collab-prod-ctl-sp ]() | `TBD` | PME | vscs-collab-prod-ctl-kv | Collaboration | Prod |

## Managed Identities

| Name | Tenant | Cert Key Vault | Component | Environment |
| --- | --- | --- | --- | --- | --- | --- |
| [vscs-codesp-dev-ctl-id ]() | CORP | Codespaces | Dev |
| [vscs-codesp-ppe-ctl-id ]() | PME | Codespaces | PPE |
| [vscs-codesp-prod-ctl-id ]() | PME | Codespaces | Prod |
| [vscs-core-dev-ctl-id ]() | CORP | Core | Dev |
| [vscs-core-ppe-ctl-id ]() | PME | Core | PPE |
| [vscs-core-prod-ctl-id ]() | PME | Core | Prod |
| [vscs-collab-dev-ctl-id ]() | CORP | Collaboration | Dev |
| [vscs-collab-ppe-ctl-id ]() | PME | Collaboration | PPE |
| [vscs-collab-prod-ctl-id ]() | PME | Collaboration | Prod |
