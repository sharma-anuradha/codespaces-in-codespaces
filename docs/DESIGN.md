# Design of a Visual Studio Services Cluster

## Definitions

**Service.** A collection of _microservices_, owned and managed by the same _service team_. Deployed and operated independently from other services.

**Service Team.** The engineering organization that owns and operates one or more services.

**Microservice.** A fine-grained component of a _service_ that can be deployed independently of other _microservices_, typically implemented as one or more co-deployed containers, together with their _microservice dependencies_.

**Microservice Dependency.** Externally provisioned resources, especially Azure resources, consumed by a  _microservice_. These are either private resources, used solely by a single _microservice_, or shared resoruces referenced by many _microservices_.

**Self-Service Cluster.** A runtime K8s environment, owned and managed by a single _service team_, into which one or more of the team's _microservices_ can be deployed.

**Local Cluster.** A K8s environment hosted on a dev box, into which all or some of a _microservice_ can be deployed for local testing.

**Multi-Tenant Cluster.** A runtime K8s environment, owned and managed by an external operations team, into which one or more _microservices_ from different _service teams_ can be deployed. _Note: VS SaaS does not support multi-tenant clusters._

## Usage Scenarios

### Scenario 1: Prototype

A new VS SaaS service, codenamed Project Purple, is being prototyped. Project Purple needs to run their _microservice_ in a development environment in Azure. They will start with a development-only prototype and, eventually, may need to deploy to production for customer-facing preview.

- The team must be able to deploy their _service_ to a development environment.
- The team may want to deploy to development environments in multiple regions, to validate multi-region operation.

### Scenario 2: Dev-Test

Valerie is a developer on Project Purple. She needs to write and debug a new feature for the service. Her work involves coding the feature, testing locally on her dev box, and finally testing her changes in a cloud environment that mimics production.

- The team must be able run and debug the _microservice_ (or a subset of it) locally.
- The team must be able to deploy one-off versions of the _service_ to a development environment.

### Scenario 3: Production

Project Purple is ready to move towards a customer-facing preview. They need to deploy their _microservice_ to the four primary VS SaaS regions for production.

## Requirements

### Service Team Autonomy

1. A _service team_ should own and operate all of the Azure resources that are provisioned specifically for their services.
1. A _service team_ must be able to manage access control for all of the Azure resources that are provisioned for their services, including JIT policy.
1. A _service team_ must be able to control their own dependencies, including the versions of dependent components and dependent services that they consume.
1. A _service team_ must be able to control their own deployment and upgrade schedules.
1. A _service team_ should not be affected by an exogenous hot-fix to a dependency, but should be able to adopt that fix independently, in a controlled and timely way.

### Deployment Requirements

1. Must be able to deploy a fully functional _self-service cluster_ to any Azure subscription in any Azure region.
1. Must be able to deploy more than one fully functional _self-service cluster_ in any Azure region.
1. Must be able to deploy and configure all containers required for the _microservice_ to any cluster.
1. Must be able to deploy one or more one-off containers to a cluster for development testing.
1. Must be able to provision and udpate _microservice dependencies_ consiting of private Azure resources consumed by the _microservice_.

### Debugging Requirements

1. Should be able to run and debug a single container locally.
1. Should be able to run and debug all containers required for the _microservice_ in a local cluster.
1. Should be able to run and debug a one-off container running in a cluster in a cloud development environment.

### Security Requirements

1. Clusters and containers running in development environment should be accessible by all team members.
1. Clusters and containers running in production environments must be secured with JIT-only access.
1. Secrets used by a _microservice_ must be managed by Azure Key Vault. [Exception is using XXX to obtain secrets and creds.]
1. Secrets used by a _microservice_ in a production environment must not be accessible except via JIT.

See [control-kubeconfig-access](https://docs.microsoft.com/en-us/azure/aks/control-kubeconfig-access).
See [authenticate-with-acr](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-aks?toc=%2Fen-us%2Fazure%2Faks%2FTOC.json&bc=%2Fen-us%2Fazure%2Fbread%2Ftoc.json).

### Observability Requirements

1. [TBD]

### Networking Requirements

1. All ingress network traffic to a VS SaaS cluster must conform to Microsoft TLS 1.2 policy.
1. All inter-node network traffic within a VS SaaS cluster must conform to Microsoft TLS 1.2 policy.
1. All egress network traffic from a VS SaaS cluster must use appropriate SSL/TLS.

## Engineering Design

### Structural Conventions

- A _service team_  owns one or more services.
- A _service_ is made up of one or more _microservices_, together with its _microservice dependencies_.
- A _service_ is deployed to one or more clusters.
- A cluster may contain one or more _microservices_.
- A _microservice_ is deployed into one cluster namespace.
- A cluster namespace contains zero or one _microservice_.

### Naming Conventions

- A subscription should be named `vssvc-{serviceName}-{env}`. For example, `vssvc-liveshare-dev`.
- A cluster should be named `vssvc-cluster-{serviceName}-{env}-{region}[-{tag}]`, with an optional redundancy tag if the deployment uses redundant clusters. For example, `vssvc-cluster-liveshare-dev-usw2`, `vssvc-liveshare-ins-usw2-1`, `vssvc-liveshare-ins-usw2-2`.
- A cluster namespace should be named after the _microservice_ it contains. For example, `liveshare-core`.

### Cluster Design and Configuration

`prefix` := `vssvc`
`baseDnsName` := `vssvc.visualstudio.com`  
`serviceName` := `liveshare` | `intellicode` | ...  
`serviceDnsName` := `{serviceName}` '.' `{baseDnsName}`  
`env` := `prod` | `ppe` | `dev`  
`instance` := `rel` | `insiders` | `staging` | `ci` | `{user}`  
`region` := `use` | `usw2` | `euw` | `asse`  

- AAD Tenant `microsoft.com`
  - Service principal `vssvc-{serviceName}-{env}-cluster-sp`
- Azure subscription `vssvc-{serviceName}-{env}` [1..*]
  - Environment resource group `vssvc-{serviceName}-{env}` [1]
    - Per-environment key vault `vssvc-{serviceName}-{env}-kv`
      - Secret `vssvc-{serviceName}-{env}-cluster-sp-appid`
      - Secret `vssvc-{serviceName}-{env}-cluster-sp-secret`
    - Per-environment DNS zone
      - Sub NS (`dev` | `ppe`) [0..*]
      - `{instance}` CNAME [1..*] -> `vssvc-{serviceName}-{env}-{instance}-tm`
    - Per-environment container registry `vssvc{servicename}{env}` [1]
  - Instance resource group `vssvc-{serviceName}-{env}-{instance}` [1..*]
    - Global traffic manager `vssvc-{serviceName}-{env}-{instance}-tm` [1]
      - Region Endpoint `{region}` [1..*] -> `vssvc-{serviceName}-{env}-{instance}-{region}-cluster`
  - Cluster resource group `vssvc-{serviceName}-{env}-{instance}-{region}-cluster` [1..*]
    - Cluster vnet `vssvc-{serviceName}-{env}-{instance}-{region}-cluster-vnet` [1]
    - Cluster network security group `vssvc-{serviceName}-{env}-{instance}-{region}-cluster-nsg` [1]
    - AKS cluster `vssvc-{serviceName}-{env}-{instance}-{region}-cluster` [1]
      - `system` namespace
      - `default` namespace
        - [helm tiller](https://github.com/helm/helm/blob/master/docs/install.md#installing-tiller)
        - Docker Secret -> `vssvc{servicename}{env}`
      - `istio-system` namespace [[link]](https://istio.io/docs/setup/kubernetes/helm-install/#option-2-install-with-helm-and-tiller-via-helm-install)
        - `istio-pilot`
        - `istio-ingressgateway`
        - `istio-policy`
        - `istio-telemetry`
        - `prometheus`
        - `istio-galley`
        - `istio-sidecar-injector`
      - `vssvc` namespace
        - Nginx load blanacer service
        - Nginx ingress controller
          - SSL/TLS Termination
        - Default http backend
        - AzSecPac daemonset
      - Microservice namespace `{microservice}` [1..*]
        - Service `{microservice}-svc`
        - Ingress `{microservice}-ing`
        - Deployment `{microservice}-dep`

### Questions about DNS mapping to Clusters

- Where does the base DNS zones live?
- Where do per-service DNS zones (subdomains) live?
- Where do per-environemnt DNS zones live? Currently in `vsengssaas-production`, outside of service team control. _This is an anti-pattern for service team autonomy._ See [intellicode](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/resourceGroups/vsengsaas-dns-zones/providers/Microsoft.Network/dnszones/intellicode.vsengsaas.visualstudio.com/overview) for a possible solution.

Solution:

- `vsengsaas-production` Azure subscription
  - `{baseDnsName}` zone
    - `{serviceName}` NS
- `vssvc-{serviceName}-prod` subscription
  - `{serviceDnsName}` zone
    - `dev` NS
    - `ppe` NS
    - `insiders` CNAME -> `vssvc-{serviceName}-prod-insiders-tm`
    - `rel` CNAME -> `vssvc-{serviceName}-prod-rel-tm`
- `vssvc-{serviceName}-ppe` subscription
  - `ppe.{serviceDnsName}` zone
    - `staging` CNAME -> `vssvc-{serviceName}-ppe-staging-tm`
- `vssvc-{serviceName}-dev` subscription
  - `dev.{serviceDnsName}` zone
    - `ci` CNAME -> `vssvc-{serviceName}-dev-ci-tm`
    - `{user}` CNAME -> `vssvc-{serviceName}-dev-{user}-tm`

Resulting FQDNs:

- `rel.{baseDnsName}`
- `insiders.{baseDnsName}`
- `staging.ppe.{baseDnsName}`
- `ci.dev.{baseDnsName}`
- `johnri.dev.{baseDnsName}`

Resource Groups

- Environment
- Instance
- Instance Cluster (region)

