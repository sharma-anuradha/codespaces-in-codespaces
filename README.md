# VS Cloud Kernel Core

The `core` service is built from VS Cloud Kernel to provide examples for many of the application patterns used by or required by other VS Cloud Kernel services.

```text
Core Service

    Networking
        + ------------------------------------------------ +
        |      dev.core.vsengsaas.visualstudio.com       |
        |              (Azure DNS Zone)                    |
        + ------------------------------------------------ +
        |                vsclk-core-tm                   |
        |           (Azure Traffic Manager)                |
        + ------------------------------------------------ +
        |            (Azure Load Balancer)                 |
        + ------------------------------------------------ +
        |            vsclk-core-cluster-vnet             |
        |            (Azure Virtual Network)               |
        + ------------------------------------------------ +
                  |
    Application   v
        + ------------------------------------------------ +
        | nginx-ingress | core.WebApi | core.DataAccess  | --- +
        + ------------------------------------------------ |     |
        |            vsclk-core-cluster                  |     |
        |        (Azure Kubernetes Service)                |     |
        + ------------------------------------------------ +     |
                          |                                      |
    Config & Storage      v                                      |
        + --------------------- +  + --------------------- +     |
        |    vsclk-core-kv    |  |    vsclk-core-db    | <-- +
        |  (Azure Key Vault)    |  |  (Azure Cosmos DB)    |
        + --------------------- +  + --------------------- +
```

Note that much of the boiler-plate Azure resources are defined in the `vsclk-cluster` project, and not in the `vsclk-core` project. These include

- DNS Zone
- Traffice Manager
- Load Balancer
- Virtual Network
- Kubernetes Service
- Key Vault
- Cosmos DB

Per-service deployment pipelines are created to deploy instances of `vsclk-cluster` as distinct service instances into the desired Azure subscription. For example, `vsclk-core-cluster` is deployed to `vsclk-core-dev` as `vsclk-core-*`.

## Project Structure

The docker images for `vsclk-core` are built under `src/docker/` and are deployed into the cluster via helm charts under `src/deploy/charts/`.

| Files | App Element |
| --- | --- |
| `.pipelines` | Azure DevOps and CDPx pipelines |
| `build/` | Build-related files, properties, targets |
| `doc/` | Documentation for this project |
| `etc/` | Stuff that doesn't get built, deployed, tested, etc. |
| `src/` | All source code |
| `src/clients/` | Client only packages, extensions, libraries |
| `src/clients/core-clients.sln` | VS IDE solution for all clients |
| `src/common/` | Libraries shared between client and service |
| `src/services/` | Service-only containers, libraries, and deployment artifacts |
| `src/services/deploy/` | Service deployment artifacts |
| `src/services/deploy/arm/` | Azure Resource Manager deployment templates |
| `src/services/deploy/charts/` | Helm charts |
| `src/services/deploy/charts/{service}/` | Helm chart for `{service}` |
| `src/services/docker/` | Root for docker images |
| `src/services/docker/docker-compose-build.yml` | Docker-compose file for docker build |
| `src/services/docker/{service}/` | Source code for `{service}` |
| `src/services/docker/{service}/Dockerfile` | Dockerfile for `{service}` image |
| `src/services/lib/{classlib}` | Private libraries shared by services |
| `test/clients/` | Unit tests for client code |
| `test/common/` | Unit tests for common code |
| `test/services/` | Unit tests for service code |
| `tools/` | Tools and scripts specific to this project |

## Getting Started

TODO:

1. Installation process
2. Software dependencies
3. Latest releases
4. API references

## Build and Test

TODO: Describe and show how to build your code and run the tests.

## Deploy

The CI pipeline for `core` deploys to [ci.dev.core.vsengsaas.visualstudio.com](https://ci.dev.core.vsengsaas.visualstudio.com).

## Contribute

Contact [cascadecore](mailto:cascadecore.microsoft.com) for information about contributing to `core` or getting started with a prototype or production service in VS Cloud Kernel.

<!-- If you want to learn more about creating good readme files then refer the following [guidelines](https://www.visualstudio.com/en-us/docs/git/create-a-readme). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore) -->
