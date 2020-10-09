variable "alias" {
  type = string
}

variable "pfs_tag" {
  type = string
}

variable "pfa_tag" {
  type = string
}

variable "errors_tag" {
  type = string
}

variable "portal_tag" {
  type = string
}

variable "pfs_registry" {
  default = "vsclkonlinedev.azurecr.io"
}

variable "portal_registry" {
  default = "vsclkonlinedev.azurecr.io"
}

variable "errors_registry" {
  default = "vsclkonlinedev.azurecr.io"
}

variable "location" {
  default = "westus2"
}

variable "subscription" {
  # vsclk-core-dev
  default = "86642df6-843e-4610-a956-fdd497102261"
}

terraform {
  backend "azurerm" {
    # Cannot use variables for backends
    subscription_id       = "86642df6-843e-4610-a956-fdd497102261"
    resource_group_name   = "pf-dev-stamp-common"
    storage_account_name  = "pftsstate"
    container_name        = "tfstate"
    key                   = "terraform.tfstate"
  }
}

provider "azurerm" {
  version         = "~> 2.14"
  subscription_id = var.subscription
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "${var.alias}-pfs"
  location = var.location
}

data "azurerm_key_vault" "kv" {
  name                = "vsclk-online-dev-kv"
  resource_group_name = "vsclk-online-dev"
}

data "azurerm_key_vault_secret" "sp_id" {
  name         = "cluster-sp-appid"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "sp_password" {
  name         = "cluster-sp-password"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "app_sp_id" {
  name         = "app-sp-appid"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "app_sp_password" {
  name         = "app-sp-password"
  key_vault_id = data.azurerm_key_vault.kv.id
}

resource "azurerm_kubernetes_cluster" "cluster" {
  name                = "${var.alias}-cluster"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  dns_prefix          = "${var.alias}-pfs"
  kubernetes_version  = "1.16.13"

  default_node_pool {
    name       = "default"
    node_count = 1
    vm_size    = "Standard_D2_v2"
  }

  role_based_access_control {
    enabled = true
  }

  service_principal {
    client_id     = data.azurerm_key_vault_secret.sp_id.value
    client_secret = data.azurerm_key_vault_secret.sp_password.value
  }

  provisioner "local-exec" {
    command = "az aks get-credentials -n ${var.alias}-cluster -g ${azurerm_resource_group.rg.name} --sub ${var.subscription} --overwrite-existing"
  }
}

variable "lock_duration" {
  default = "PT30S"
}

variable "requires_duplicate_detection" {
  default = true
}

variable "max_size_in_megabytes" {
  default = 4096
}

variable "requires_session" {
  default = true
}

variable "default_message_ttl" {
  default = "PT10M"
}

variable "duplicate_detection_history_time_window" {
  default = "PT30S"
}

variable "max_delivery_count" {
  default = 3
}

resource "azurerm_servicebus_namespace" "sb" {
  name                = "${var.alias}sb"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "connections_new" {
  name                = "connections-new"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name

  lock_duration                           = var.lock_duration
  requires_duplicate_detection            = var.requires_duplicate_detection
  max_size_in_megabytes                   = var.max_size_in_megabytes
  requires_session                        = var.requires_session
  default_message_ttl                     = var.default_message_ttl
  duplicate_detection_history_time_window = var.duplicate_detection_history_time_window
  max_delivery_count                      = var.max_delivery_count
}

resource "azurerm_servicebus_queue" "connections_established" {
  name                = "connections-established"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name

  lock_duration                           = var.lock_duration
  requires_duplicate_detection            = var.requires_duplicate_detection
  max_size_in_megabytes                   = var.max_size_in_megabytes
  requires_session                        = var.requires_session
  default_message_ttl                     = var.default_message_ttl
  duplicate_detection_history_time_window = var.duplicate_detection_history_time_window
  max_delivery_count                      = var.max_delivery_count
}

resource "azurerm_servicebus_queue" "connections_errors" {
  name                = "connections-errors"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name

  lock_duration                           = var.lock_duration
  requires_duplicate_detection            = var.requires_duplicate_detection
  max_size_in_megabytes                   = var.max_size_in_megabytes
  requires_session                        = var.requires_session
  default_message_ttl                     = var.default_message_ttl
  duplicate_detection_history_time_window = var.duplicate_detection_history_time_window
  max_delivery_count                      = var.max_delivery_count
}

provider "kubernetes" {
  version = "~> 1.11"

  load_config_file       = "false"
  host                   = azurerm_kubernetes_cluster.cluster.kube_config.0.host
  username               = azurerm_kubernetes_cluster.cluster.kube_config.0.username
  password               = azurerm_kubernetes_cluster.cluster.kube_config.0.password
  client_certificate     = base64decode(azurerm_kubernetes_cluster.cluster.kube_config.0.client_certificate)
  client_key             = base64decode(azurerm_kubernetes_cluster.cluster.kube_config.0.client_key)
  cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.cluster.kube_config.0.cluster_ca_certificate)
}

provider "tls" {
  version = "~> 2.1"
}

provider "local" {
  version = "~> 1.4"
}

provider "helm" {
  version = "~> 1.2"
  kubernetes {
    load_config_file       = "false"
    host                   = azurerm_kubernetes_cluster.cluster.kube_config.0.host
    username               = azurerm_kubernetes_cluster.cluster.kube_config.0.username
    password               = azurerm_kubernetes_cluster.cluster.kube_config.0.password
    client_certificate     = base64decode(azurerm_kubernetes_cluster.cluster.kube_config.0.client_certificate)
    client_key             = base64decode(azurerm_kubernetes_cluster.cluster.kube_config.0.client_key)
    cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.cluster.kube_config.0.cluster_ca_certificate)
  }
}

resource "helm_release" "nginx" {
  name  = "nginx-ingress-controller"
  chart = "./nginx-ingress"

  set {
    name  = "clusterName"
    value = azurerm_kubernetes_cluster.cluster.name
  }
}

resource "tls_private_key" "ssl_cert" {
  algorithm = "ECDSA"
}

resource "tls_self_signed_cert" "ssl_cert" {
  key_algorithm   = tls_private_key.ssl_cert.algorithm
  private_key_pem = tls_private_key.ssl_cert.private_key_pem

  # Certificate expires after 12 hours.
  validity_period_hours = 8760

  # Generate a new certificate if Terraform is run within three
  # hours of the certificate's expiration time.
  early_renewal_hours = 3

  # Reasonable set of uses for a server SSL certificate.
  allowed_uses = [
    "key_encipherment",
    "digital_signature",
    "server_auth",
  ]

  dns_names = [
    "online.dev.core.vsengsaas.visualstudio.com",
    "*.app.online.dev.core.vsengsaas.visualstudio.com",
    "*.apps.dev.codespaces.githubusercontent.com",
    "*.dev.github.dev",
  ]

  subject {
    common_name  = "online.dev.core.vsengsaas.visualstudio.com"
    organization = "VSCS Dev"
  }
}

resource "kubernetes_secret" "ssl_cert" {
  metadata {
    name = "ssl-cert"
  }

  data = {
    "tls.crt" = tls_self_signed_cert.ssl_cert.cert_pem
    "tls.key" = tls_private_key.ssl_cert.private_key_pem
  }

  type = "kubernetes.io/tls"
}

resource "kubernetes_secret" "ssl_cert_github_cs_pf" {
  metadata {
    name = "ssl-cert-github-cs-pf"
  }

  data = {
    "tls.crt" = tls_self_signed_cert.ssl_cert.cert_pem
    "tls.key" = tls_private_key.ssl_cert.private_key_pem
  }

  type = "kubernetes.io/tls"
}

resource "kubernetes_secret" "ssl_cert_github_dot_dev" {
  metadata {
    name = "ssl-cert-github-dot-dev"
  }

  data = {
    "tls.crt" = tls_self_signed_cert.ssl_cert.cert_pem
    "tls.key" = tls_private_key.ssl_cert.private_key_pem
  }

  type = "kubernetes.io/tls"
}

resource "kubernetes_secret" "web_secrets" {
  metadata {
    name = "web-port-forwarding-web-api-settings"
  }

  data = {
    "APPSECRETS__APPSERVICEPRINCIPALCLIENTSECRET" = data.azurerm_key_vault_secret.app_sp_password.value
    "ASPNETCORE_ENVIRONMENT"                      = "Development"
    "RUNNING_IN_AZURE"                            = "true"
    "OVERRIDE_APPSETTINGS_JSON"                   = "appsettings.dev-ci.json"
  }

  type = "Opaque"
}

resource "kubernetes_secret" "errors_backend_secrets" {
  metadata {
    name = "web-errors-backend-settings"
  }

  data = {
    "APPSECRETS__APPSERVICEPRINCIPALCLIENTSECRET" = data.azurerm_key_vault_secret.app_sp_password.value
    "ASPNETCORE_ENVIRONMENT"                      = "Development"
    "RUNNING_IN_AZURE"                            = "true"
    "OVERRIDE_APPSETTINGS_JSON"                   = "appsettings.dev-ci.json"
  }

  type = "Opaque"
}

# Web includes latest PFA
resource "helm_release" "web" {
  name  = "web"
  chart = "../../bin/debug/Deploy/web"

  values = [
    file("../../bin/debug/Deploy/web/values.dev-alias.yaml")
  ]

  set {
    name  = "port-forwarding-web-api.image.repositoryUrl"
    value = var.pfs_registry
  }

  set {
    name  = "port-forwarding-web-api.image.tag"
    value = var.pfs_tag
  }

  set {
    name  = "port-forwarding-web-api.image.agentTag"
    value = var.pfa_tag
  }

  set {
    name  = "port-forwarding-web-api.image.pullPolicy"
    value = "Always"
  }

  set {
    name  = "port-forwarding-web-api.serviceBus.overrideDev"
    value = true
  }

  set {
    name  = "port-forwarding-web-api.serviceBus.namespace"
    value = "${var.alias}sb"
  }

  set {
    name  = "port-forwarding-web-api.serviceBus.resourceGroup"
    value = azurerm_resource_group.rg.name
  }

  set {
    name  = "errors-backend.image.repositoryUrl"
    value = var.errors_registry
  }

  set {
    name  = "errors-backend.image.tag"
    value = var.errors_tag
  }

  set {
    name  = "errors-backend.image.pullPolicy"
    value = "Always"
  }
}

data "azurerm_key_vault_secret" "AesKey" {
  name         = "Config-AesKey"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "AesIV" {
  name         = "Config-AesIV"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "MicrosoftAppClientSecret" {
  name         = "Config-MicrosoftAppClientSecret"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "RedisConnectionString" {
  name         = "Config-RedisConnectionString"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "SignalRConnectionString" {
  name         = "Config-SignalRConnectionString"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "GitHubAppClientSecret" {
  name         = "Config-GitHubAppClientSecret"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "GitHubNativeAppClientSecret" {
  name         = "Config-GitHubNativeAppClientSecret"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "GitHubAzurePortalClientSecret" {
  name         = "Config-GitHubAzurePortalClientSecret"
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "AzDevAppClientSecret" {
  name         = "Config-AzDevAppClientSecret"
  key_vault_id = data.azurerm_key_vault.kv.id
}

resource "kubernetes_secret" "portal_secrets" {
  metadata {
    name = "portal-vsclk-portal-website-settings"
  }

  data = {
    "ASPNETCORE_ENVIRONMENT"                                             = "Development"
    "PORTAL_AppSettings__AesIV"                                          = data.azurerm_key_vault_secret.AesIV.value
    "PORTAL_AppSettings__AesKey"                                         = data.azurerm_key_vault_secret.AesKey.value
    "PORTAL_AppSettings__ApiEndpoint"                                    = "https://online.dev.core.vsengsaas.visualstudio.com/api/v1"
    "PORTAL_AppSettings__AuthRedirectUrl"                                = "https://online.dev.core.vsengsaas.visualstudio.com"
    "PORTAL_AppSettings__AzDevAppClientId"                               = "B4074F41-EC85-4D4E-A3CA-4458F1A904CC"
    "PORTAL_AppSettings__AzDevAppClientSecret"                           = data.azurerm_key_vault_secret.AzDevAppClientSecret.value
    "PORTAL_AppSettings__Domain"                                         = "online.dev.core.vsengsaas.visualstudio.com"
    "PORTAL_AppSettings__EnvironmentRegistrationEndpoint"                = "https://online.dev.core.vsengsaas.visualstudio.com/api/v1/environments"
    "PORTAL_AppSettings__GitHubAppClientId"                              = "fe42cbbb45cdcb17a106"
    "PORTAL_AppSettings__GitHubAppClientSecret"                          = data.azurerm_key_vault_secret.GitHubAppClientSecret.value
    "PORTAL_AppSettings__GitHubAzurePortalClientId"                      = "85b34cc4079b954b09fc"
    "PORTAL_AppSettings__GitHubAzurePortalClientSecret"                  = data.azurerm_key_vault_secret.GitHubAzurePortalClientSecret.value
    "PORTAL_AppSettings__GitHubNativeAppClientId"                        = "Iv1.faf5804e51bc0d8f"
    "PORTAL_AppSettings__GitHubNativeAppClientSecret"                    = data.azurerm_key_vault_secret.GitHubNativeAppClientSecret.value
    "PORTAL_AppSettings__GitHubPortForwardingDomainTemplate"             = "{0}.apps.dev.codespaces.githubusercontent.com"
    "PORTAL_AppSettings__GitHubPortForwardingEnableEnvironmentEndpoints" = "true"
    "PORTAL_AppSettings__GitHubPortForwardingServiceEnabled"             = "true"
    "PORTAL_AppSettings__GitHubPortForwardingManagementEndpoint"         = "https://management.apps.dev.codespaces.githubusercontent.com/api/v1/PortForwardingConnections"
    "PORTAL_AppSettings__KeyVaultName"                                   = "vsclk-online-dev-kv"
    "PORTAL_AppSettings__KeyVaultReaderServicePrincipalClientId"         = data.azurerm_key_vault_secret.app_sp_id.value
    "PORTAL_AppSettings__KeyVaultReaderServicePrincipalClientSecret"     = data.azurerm_key_vault_secret.app_sp_password.value
    "PORTAL_AppSettings__LiveShareEndpoint"                              = "https://ppe.liveshare.vsengsaas.visualstudio.com"
    "PORTAL_AppSettings__LiveShareWebExtensionEndpoint"                  = "https://vslsdev.blob.core.windows.net/webextension"
    "PORTAL_AppSettings__MicrosoftAppClientId"                           = "ce5aa96e-1717-4055-a2b5-cfe4aebbf36d"
    "PORTAL_AppSettings__MicrosoftAppClientSecret"                       = data.azurerm_key_vault_secret.MicrosoftAppClientSecret.value
    "PORTAL_AppSettings__PortForwardingDomainTemplate"                   = "{0}.app.online.dev.core.vsengsaas.visualstudio.com"
    "PORTAL_AppSettings__PortForwardingManagementEndpoint"               = "https://management.app.online.dev.core.vsengsaas.visualstudio.com/api/v1/PortForwardingConnections"
    "PORTAL_AppSettings__PortForwardingEnableEnvironmentEndpoints"       = "true"
    "PORTAL_AppSettings__PortForwardingServiceEnabled"                   = "true"
    "PORTAL_AppSettings__PortalEndpoint"                                 = "https://online.dev.core.vsengsaas.visualstudio.com"
    "PORTAL_AppSettings__RichNavWebExtensionEndpoint"                    = "https://intellinavstgdev.blob.core.windows.net/webextension"
    "PORTAL_AppSettings__VsClkRedisConnectionString"                     = data.azurerm_key_vault_secret.RedisConnectionString.value
    "PORTAL_AppSettings__VsClkSignalRConnectionString"                   = data.azurerm_key_vault_secret.SignalRConnectionString.value
    "PORTAL_AppSettings__VsSaaSCertificateSecretName"                    = "vsls-ppe-auth-cert-primary"
    "PORTAL_AppSettings__VsSaaSTokenIssuer"                              = "https://ppe.liveshare.vsengsaas.visualstudio.com/"
    "PORTAL_AppSettings__VsSaaSTokenCertsEndpoint"                       = "https://sts.dev.core.vsengsaas.visualstudio.com"
  }

  type = "Opaque"
}

# Web includes latest PFA
resource "helm_release" "portal" {
  name  = "portal"
  chart = "../../src/services/containers/VsClk.Portal.WebSite/Charts/vsclk-portal-website"

  values = [
    file("../../src/services/deploy/charts/common/vsclk-online-dev-alias.yaml")
  ]

  set {
    name  = "image.repositoryUrl"
    value = var.portal_registry
  }

  set {
    name  = "image.pullPolicy"
    value = "Always"
  }

  set {
    name  = "image.tag"
    value = var.portal_tag
  }

  set {
    name  = "pods.replicaCount"
    value = 1
  }

  set {
    name  = "configuration.isDevStamp"
    value = 1
  }
}

data "kubernetes_service" "nginx-controller" {
  metadata {
    name = "service-frontend-lb"
  }
  depends_on = [
    helm_release.nginx
  ]
}

resource "local_file" "env_file" {
  content = join("\n", [
    "PORTAL_ORIGIN=https://${data.kubernetes_service.nginx-controller.load_balancer_ingress.0.ip}",
    "PORT_FORWARDING_ORIGIN=https://${data.kubernetes_service.nginx-controller.load_balancer_ingress.0.ip}"
  ])

  filename = "../../src/Portal/PortalWebsite/Src/dev-stamp.env"
}

resource "local_file" "chart_values_web" {
  content = join("\n", [
    "port-forwarding-web-api:",
    "  image:",
    "    repositoryUrl: ${var.pfs_registry}",
    "    tag: ${var.pfs_tag}",
    "    pullPolicy: Always",
    "    agentTag: ${var.pfa_tag}",
    "  serviceBus:",
    "    overrideDev: true",
    "    namespace: ${var.alias}sb",
    "    resourceGroup: ${azurerm_resource_group.rg.name}",
    ""
  ])

  filename = "../../src/Deploy/web/values.dev-generated.yaml"
}

resource "local_file" "chart_values_web_bin" {
  content = join("\n", [
    "port-forwarding-web-api:",
    "  image:",
    "    repositoryUrl: ${var.pfs_registry}",
    "    tag: ${var.pfs_tag}",
    "    pullPolicy: Always",
    "    agentTag: ${var.pfa_tag}",
    "  serviceBus:",
    "    overrideDev: true",
    "    namespace: ${var.alias}sb",
    "    resourceGroup: ${azurerm_resource_group.rg.name}",
    "",
    "errors-backend:",
    "  image:",
    "    repositoryUrl: ${var.errors_registry}",
    "    tag: ${var.errors_tag}",
    "    pullPolicy: Always",
    ""
  ])

  filename = "../../bin/debug/Deploy/web/values.dev-generated.yaml"
}

resource "local_file" "chart_values_portal" {
  content = join("\n", [
    "image:",
    "  repositoryUrl: ${var.pfs_registry}",
    "  tag: ${var.portal_tag}",
    "  pullPolicy: Always",
    "pods:",
    "  replicaCount: 1",
    "configuration:",
    "  isDevStamp: 1",
    ""
  ])

  filename = "../../src/services/deploy/charts/common/values.dev-generated.yaml"
}

output "custer_context_command" {
  value = "az aks get-credentials -n ${azurerm_kubernetes_cluster.cluster.name} -g ${azurerm_resource_group.rg.name} --sub ${var.subscription}"
}

output "update_web_chart" {
  value = "helm upgrade web ../../bin/debug/Deploy/web -f ../../bin/debug/Deploy/web/values.dev-alias.yaml -f ../../bin/debug/Deploy/web/values.dev-generated.yaml"
}

output "update_portal_chart" {
  value = "helm upgrade portal ../../src/services/containers/VsClk.Portal.WebSite/Charts/vsclk-portal-website -f ../../src/services/deploy/charts/common/vsclk-online-dev-alias.yaml -f ../../src/services/deploy/charts/common/values.dev-generated.yaml"
}

output "cluster_external_ip" {
  value = data.kubernetes_service.nginx-controller.load_balancer_ingress.0.ip
}

output "serviceBusResourceGroupName" {
  value = azurerm_resource_group.rg.name
}

output "serviceBusNamespaceName" {
  value = azurerm_servicebus_namespace.sb.name
}