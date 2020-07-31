# Ev2 / SubSystems / Codespaces

The Codespaces service.

## Subscriptions

- Control-plane default: `vscs-cdsp-{env}`
- Control-plane images: `vscs-cdsp-{env}-images`
- Control-plane partner integration: `vscs-cdsp-{env}-github`

## Deployments

- Control-plane default azure resources
  - Container registry, Cosmos DB, storage accounts, etc.
- Control-plane partner azure resources
  - Cosmos DB or storage account?
- Control-plane k8s appliction -> Core cluster | Default namespace or "Codespaces" namespace?
  - Codespaces public ingress
  - Codespaces pod: front-end web api
  - Codespaces pod: front-end task manager
  - Codespaces pod: front-end task workers
  - Codespaces pod: resource broker
  - Codespaces pod: resource broker task manager
  - Codespaces pod: resource broker task workers
- Data-plane images azure resources
  - *Are this properly data-plane resources?*
  - Container registry (for linux images) {X data-plane location}
  - Shared image gallery {X data-plane location}
- Data-plane Docker images (Linux)
  - *Are this properly data-plane resources?*
  - from GitHub action, currently in "devcon"
- Data-plane VM images (Windows)
  - *Are this properly data-plane resources?*
  - from image pipelines {X data-plane location}

## Depends On

- [Core](../Core/README.md)
- [Resource Broker](../ResourceBroker/README.md)
- [Collaboration](../Collaboration/README.md)
