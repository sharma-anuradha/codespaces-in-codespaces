# Port Forwarding Dev Stamp

Files in this folder take care of developer stamp setup for port forwarding.

tl;dr version

Good way to run this setup is from a [new Codespace](https://online.visualstudio.com/environments/new?repo=https%3A%2F%2Fdevdiv.visualstudio.com%2FDefaultCollection%2FOnlineServices%2F_git%2Fvsclk-core&name=pfs-vsclk-core) (yes, it's a deeplink that will help you create one).

> When done, don't forget to download your `src/Portal/PortalWebsite/Src/dev-stamp.env` file to point your local nginx to the right place.

Prerequisities
1. Azure CLI - [How to install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
1. Terraform CLI - [How to install](https://learn.hashicorp.com/terraform/getting-started/install)
1. Docker & Docker Compose - [How to install](https://docs.docker.com/compose/install/)

Then do:

1. run `./setup.sh --alias <your_alias>`
1. Set your local development nginx to run against IP address you get as a result of the setup (e.g. cluster_external_ip = 52.183.25.204).
``` sh
# 1. Move to Portal folder
cd ../../src/Portal/PortalWebsite/Src/Website/

# 2. (Optional if ran before)
yarn && yarn setup

# 3. Start local DNS server and nginx in detached mode
docker-compose --env-file ../dev-stamp.env up --build -d
```

To use custom built images for either one of portal, PFA, PFS, you can push them to vsclkonlinedev ACR and use the tags for example like this.

``` sh
./setup.sh --alias pelisy --pfs-tag dev-pelisy-001 --portal-tag dev-pelisy-002 --pfa-tag dev-pelisy-003
```

> If none of the tags are specifier, we ask ACR for latest available tag that looks like it was published by build pipelines (e.g. `0.1.01266.0005-alpha-b69c2334`) and use that as default. This way, every time you run the setup script, you'll get latest and greatest built images.

## New Infrastructure

The terraform will access and create some azure resources.

1. Resource group `<alias>-pfs`
1. AKS Cluster `<alias>-cluster`
1. Service Bus Namespace `<alias>sb`
    - "connections-new" queue
    - "connections-established" queue

> The first time the AKS Cluster is created, terraform will also set current kubectl context to the new cluster for convenience

Then it will setup the Kubernetes cluster by installing

1. Nginx Ingress Controller
1. Portal
1. Port Forwarding Service
1. Port Forwarding Agent

> PFS and PFA in this cluster will use the new SB to talk to each other.

Since not all systems support helm plugin for fetching keyvault secrets, terraform will create the secrets without requiring the helm plugin.

## Building Debug Images

To be able to attach debugger to running pods in the cluster you need special images with tooling set up so the kubernetes extension can pick the process and attach to debugger server.

### Port Forwarding Web API (or PFS)

There's a VSCode task set up for building PFS debug image in root of vsclk-core repo.

1. Ctrl+Shift+b to open build task picker. 
1. Pick `pfs:docker-build:debug. This will create a `port-forwarding-web-api:debug` docker image locally. You need to push the image to `vsclkonlinedev.azurecr.io` registry.
1. Tag the image as `docker tag port-forwarding-web-api:debug vsclkonlinedev.azurecr.io/port-forwarding-web-api:dev-<alias>-<unique_id>` (e.g. vsclkonlinedev.azurecr.io/port-forwarding-web-api:dev-pelisy-042`)
1. (Optional) Authenticate docker to push to the Azure Container Registry (ACR) `az acr login --name vsclkonlinedev`.
1. Push the image to the registry `docker push vsclkonlinedev.azurecr.io/port-forwarding-web-api:dev-<alias>-<unique_id>`.
1. Update your kubernetes cluster by either running the setup script with `--pfs-tag dev-<alias>-<unique_id>` or talking to kubernetes directly `kubectl set image deployment/web-port-forwarding-web-api port-forwarding-web-api=vsclkonlinedev.azurecr.io/port-forwarding-web-api:dev-<alias>-<unique_id> --record`

### Portal

There's a shell script to build Portal debug image in `build/portal/build.debug.sh`.

#### Prerequisites
1. Restore NuGet packages `dotnet restore dirs.proj --interactive`
1. Install node modules `cd src/Portal/PortalWebsite/Src/Website/` and `yarn install`

#### Building the image

1. Run the `build/portal/build.debug.sh` shell script. This will create a `vsclk.portal.website:debug` docker image locally. You need to push the image to `vsclkonlinedev.azurecr.io` registry.
1. Tag the image as `docker tag vsclk.portal.website:debug vsclkonlinedev.azurecr.io/vsclk.portal.website:dev-<alias>-<unique_id>` (e.g. vsclkonlinedev.azurecr.io/vsclk.portal.website:dev-pelisy-042`)
1. (Optional) Authenticate docker to push to the Azure Container Registry (ACR) `az acr login --name vsclkonlinedev`.
1. Push the image to the registry `docker push vsclkonlinedev.azurecr.io/vsclk.portal.website:dev-<alias>-<unique_id>`.
1. Update your kubernetes cluster by either running the setup script with `--portal-tag dev-<alias>-<unique_id>` or talking to kubernetes directly `kubectl set image deployment/portal-vsclk-portal-website vsclk-portal-website=vsclkonlinedev.azurecr.io/vsclk.portal.website:dev-<alias>-<unique_id> --record`
