# Core Azure Resources

## Subscriptions

- vscs-core-{env}-ops
- vscs-core-{env}-ctl
- vscs-core-{env}-data-{geo}-infra-001
- vscs-core-{env}-data-{geo}-compute-###
- vscs-core-{env}-data-{geo}-storage-###

## Per-Environment Ops

## Per-Environment Control

- Container Registry --> should be per region
- Core Key Vault --> core
- STS Key Vault --> core
- Billing Key Vault --> core
- Virtual Network (unused!)

## Per-Instance Control

- Virtual Network --> core
- Instance Cosmos DB --> core, ref vnet
- Instance Maps (shared/core)
- Instance traffic manager profile

## Per-Region (Stamp)

- Cluster NSG
- Cluster VNet
- AKS Cluster, ref vnet
- Stamp Cosmos DB, ref vent
- [stamp storage account *sa] (unused?) --> core
- [stamp storage account *rrrrXXsa] --> core

## Per-Geography Data

- VM Image Galleries
- Container Registry
- Archive Storage
