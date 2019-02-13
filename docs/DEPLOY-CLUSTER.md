# Bootstrapping a Cluster

- **Create the cluster environment**
  - Create the environment resource group: `vssvc-{serviceName}-{env}`
  - Create the environment key vault: `vssvc-{serviceName}-{env}-kv`
  - Create the environment container registry: `vssvc{servicename}{env}`
- **Create a cluster service principal**
  - `password=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 128 | head -n 1)`
  - `sp=$(az ad sp create-for-rbac --skip-assignment --name vssvc-{serviceName}-{env}-cluster-sp --password $password)`
  - **Add the service principal secrets to the key vault**
    - `cluster-sp-appid`: `appid=echo $sp | jq .appId`
    - `cluster-sp-name` : `name=echo $sp | jq .name`
    - `cluster-sp-password`: `password=echo $sp | jq .password`
    - `cluster-sp-tenant`: `tenant=echo $sp | jq .tenant`
  - **Set the password secret expiration**
    - `exp="$(date -u -d "+1 year" -Ihours)Z"`
    - `az keyvault secret set-attributes --vault-name $vault --name cluster-sp-password --expires $exp`
- **Create the AKS cluster**
  - Create instance resource group" `vssvc-{serviceName}-{env}-{instance}`
  - `vssvc-{serviceName}-{env}-{instance}-{region}-cluster`
  - Use sevice principal `vssvc-{serviceName}-{env}-cluster-sp`
- **Connect to the cluster**
  - `az aks get-credentials --resource-group vssvc-{serviceName}-{env}-{instance} --name vssvc-{serviceName}-{env}-{instance}-{region}-cluster`
  - `kubectl cluster-info`
- **Initialize cluster RBAC and custom pod security policy**
  - `kubectl apply src/k8s/custom-default-psp-role.yml`
  - `kubectl apply src/k8s/custom-default-psp-rolebinding.yml`
  - `kubectl apply src/k8s/custom-default-psp.yml`
  - `kubectl apply src/k8s/nginx-ingress-clusterrole.yml`
  - `kubectl apply src/k8s/nginx-ingress-clusterrolebinding.yml`
  - `kubectl apply src/k8s/tiller-sa.yml`
  - `kubectl apply src/k8s/tiller-sa-clusterrolebinding.yml`
- **Initialize Helm-Tiller**
  - Set up rbac for helm-tiller
  - `helm init --service-account tiller-sa`
- **Install Nginx Chart**
  - `helm install src/charts/nginx-ingress -n nginx-ingress`
- **Configure LoadBalancer IP Address & DNS Names**

| _Consider the **application object** as the global representation of your application for use across all tenants, and the **service principal** as the local representation for use in a specific tenant._