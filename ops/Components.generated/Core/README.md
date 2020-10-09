# Ev2 / SubSystem / Core

## Ops Plane

- Ops subscription + RBAC
- Ops service principal + ADO service connections
- Ops key vault, monitoring certificates++, secrets
- Ops container registry, build-time image repositories

## Control Plane

- Ctl subscription + subscription RBAC
- Ctl key vault, SSL certificates++, secrets
- Ctl DNS zone, cnames
- Ctl traffic manager profiles, endpoints

### Control Plane Kubernetes Cluster

- Ctl container registry, runtime image repositories
- Ctl cluster nsg, vnet, static IP address
- Ctl kubernetes service + managed identity
- Ctl kubernetes cluster configuration
  - Cluster RBAC
  - NGINX ingress controller
  - Kured
  - Docker image cleanup
  - Geneva Logger
  - AzSecPack Daemon
  - SSL certificate secret

## Data Plane

- N/A

## Old certs that need to be ported or re-created

Certificate codespaces-mdm --> ops key vault
Certificate vsclk-core-dev-monitoring --> ops key vault!
Certificate vsls-dev-monitoring --> ops key vault!
Certificate azureportalextension-cert --> codespaces?
Certificate azureportalextension-ev2cert --> codespaces?
Certificate dev-codespaces-githubusercontent-com-ssl --> codespaces
Certificate dev-core-vsengsaas-visualstudio-com-ssl --> codespaces
Certificate dev-github-dev-ssl --> codespaces
