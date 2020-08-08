// <copyright file="KubernetesAgentMappingClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings
{
    /// <inheritdoc/>
    public class KubernetesAgentMappingClient : IAgentMappingClient
    {
        private const string DefaultNamespace = "default";
        private const int MappingServicePort = 80;
        private const string NameLabelKey = "vsonline.port-forwarding/agent-name";
        private const string UidLabelKey = "vsonline.port-forwarding/agent-uid";
        private const string DeploymentNameLabelKey = "app.kubernetes.io/name";
        private readonly Dictionary<string, string> nginxIngressAnnotations = new Dictionary<string, string>
        {
            ["kubernetes.io/ingress.class"] = "nginx",
            ["nginx.ingress.kubernetes.io/auth-url"] = "http://portal-vsclk-portal-website.default.svc.cluster.local/auth",
            ["nginx.ingress.kubernetes.io/auth-signin"] = "/signin?cid=$request_id",
            ["nginx.ingress.kubernetes.io/upstream-vhost"] = "localhost",
            ["nginx.ingress.kubernetes.io/configuration-snippet"] = @"
                set $new_cookie $http_cookie;

                if ($new_cookie ~ ""(.*)(?:^|;)\s*use_vso_pfs=[^;]+(.*)"") {
                    set $new_cookie $1$2;
                }
                if ($new_cookie ~ ""(.*)(?:^|;)\s*VstsSession=[^;]+(.*)"") {
                    set $new_cookie $1$2;
                }
                if ($new_cookie ~ ""(.*)(?:^|;)\s*__Host-vso-pf=[^;]+(.*)"") {
                    set $new_cookie $1$2;
                }
                if ($new_cookie ~ "";\s*(.+);?\s*"") {
                    set $new_cookie $1;
                }

                # Enable webpack-dev-server live reload without allowing PF host on dev side.
                proxy_set_header origin ""http://localhost"";
                proxy_set_header Cookie $new_cookie;
            ".Replace("\r\n", "\n").Replace($"                ", string.Empty).Trim('\n', ' '),
            ["nginx.ingress.kubernetes.io/server-snippet"] = @"
                location /signin {
                    set $http_correlation_id $request_id;

                    if ($arg_cid) {
                        set $http_correlation_id $arg_cid;
                    }

                    proxy_set_header X-Request-ID $http_correlation_id;
                    proxy_pass http://portal-vsclk-portal-website.default.svc.cluster.local;
                }
                location /authenticate-codespace {
                    proxy_pass_request_body on;
                    proxy_pass 'http://portal-vsclk-portal-website.default.svc.cluster.local';
                    proxy_set_header Host $http_host;

                    proxy_buffer_size          128k;
                    proxy_buffers              4 256k;
                    proxy_busy_buffers_size    256k;
                }
            ".Replace("\r\n", "\n").Replace($"                ", string.Empty).Trim('\n', ' '), // We clean up indentation and normalize new lines for kubernetes use
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="KubernetesAgentMappingClient"/> class.
        /// </summary>
        /// <param name="appSettings">The service settings.</param>
        /// <param name="kubernetesClient">The kubernetes client.</param>
        public KubernetesAgentMappingClient(PortForwardingAppSettings appSettings, IKubernetes kubernetesClient)
        {
            KubernetesClient = Requires.NotNull(kubernetesClient, nameof(kubernetesClient));
            AppSettings = Requires.NotNull(appSettings, nameof(appSettings));
        }

        private IKubernetes KubernetesClient { get; }

        private PortForwardingAppSettings AppSettings { get; }

        /// <inheritdoc/>
        public Task RegisterAgentAsync(AgentRegistration registration, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_register_agent",
                async (childLogger) =>
                {
                    childLogger.AddAgentRegistration(registration);

                    var podList = await KubernetesClient.ListNamespacedPodAsync(DefaultNamespace, fieldSelector: $"metadata.name={registration.Name}");
                    var namedPod = podList.Items.SingleOrDefault();
                    if (namedPod == default)
                    {
                        throw new ArgumentException($"{nameof(registration)} does not represent existing Pod in \"{DefaultNamespace}\" namespace.");
                    }

                    var updatedLabels = new Dictionary<string, string>(namedPod.Metadata.Labels)
                    {
                        [NameLabelKey] = registration.Name,
                        [UidLabelKey] = registration.Uid,
                    };
                    var patch = new JsonPatchDocument<V1Pod>();
                    patch.Replace(p => p.Metadata.Labels, updatedLabels);

                    await KubernetesClient.PatchNamespacedPodAsync(new V1Patch(patch), registration.Name, DefaultNamespace);
                });
        }

        /// <inheritdoc/>
        public async Task CreateAgentConnectionMappingAsync(ConnectionDetails mapping, IDiagnosticsLogger logger)
        {
            if (mapping == default)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            logger.AddConnectionDetails(mapping);

            await Task.WhenAll(
                logger.OperationScopeAsync(
                    "kubernetes_create_agent_endpoint",
                    async (childLogger) =>
                    {
                        childLogger.AddValue("KubernetesEndpointName", mapping.GetKubernetesServiceName());

                        var agentPod = await KubernetesClient.ReadNamespacedPodAsync(mapping.AgentName, DefaultNamespace);

                        var endpoints = new V1Endpoints
                        {
                            Metadata = new V1ObjectMeta
                            {
                                Name = mapping.GetKubernetesServiceName(),
                            },
                            Subsets = new[]
                            {
                                new V1EndpointSubset
                                {
                                    Addresses = new[]
                                    {
                                        new V1EndpointAddress { Ip = agentPod.Status.PodIP, TargetRef = mapping.ToObjectReference() },
                                    },
                                    Ports = new[]
                                    {
                                        new V1EndpointPort(mapping.DestinationPort),
                                    },
                                },
                            },
                        };

                        await KubernetesClient.CreateNamespacedEndpointsAsync(endpoints, DefaultNamespace);
                    },
                    swallowException: true),
                logger.OperationScopeAsync(
                    "kubernetes_create_agent_service",
                    async (childLogger) =>
                    {
                        childLogger.AddValue("KubernetesServiceName", mapping.GetKubernetesServiceName());

                        var service = new V1Service
                        {
                            ApiVersion = "v1",
                            Kind = "Service",
                            Metadata = new V1ObjectMeta
                            {
                                Name = mapping.GetKubernetesServiceName(),
                                OwnerReferences = new List<V1OwnerReference> { mapping.ToOwnerReference() },
                            },
                            Spec = new V1ServiceSpec
                            {
                                Ports = new List<V1ServicePort>
                                {
                                    new V1ServicePort { Port = MappingServicePort, TargetPort = mapping.DestinationPort },
                                },
                            },
                        };

                        await KubernetesClient.CreateNamespacedServiceAsync(service, DefaultNamespace);
                    },
                    swallowException: true),
                logger.OperationScopeAsync(
                    "kubernetes_create_agent_ingress",
                    async (childLogger) =>
                    {
                        var rules = AppSettings.HostsConfigs
                            .SelectMany(hostConf =>
                            {
                                return hostConf.Hosts.Select(host => new Extensionsv1beta1IngressRule
                                {
                                    Host = string.Format(host, mapping.GetPortForwardingSessionSubdomain()),
                                    Http = new Extensionsv1beta1HTTPIngressRuleValue
                                    {
                                        Paths = new List<Extensionsv1beta1HTTPIngressPath>
                                    {
                                        new Extensionsv1beta1HTTPIngressPath
                                        {
                                            Backend = new Extensionsv1beta1IngressBackend(
                                                serviceName: mapping.GetKubernetesServiceName(),
                                                servicePort: MappingServicePort),
                                            Path = "/",
                                        },
                                    },
                                    },
                                });
                            })
                            .ToList();

                        var tls = AppSettings.HostsConfigs
                            .Select(config => new Extensionsv1beta1IngressTLS
                            {
                                SecretName = config.CertificateSecretName,
                                Hosts = config.Hosts.Select(h => string.Format(h, "*")).ToList(),
                            })
                            .ToList();

                        var ingress = new Extensionsv1beta1Ingress
                        {
                            ApiVersion = "extensions/v1beta1",
                            Kind = "Ingress",
                            Metadata = new V1ObjectMeta
                            {
                                Name = mapping.GetKubernetesServiceName(),
                                OwnerReferences = new List<V1OwnerReference> { mapping.ToOwnerReference() },
                                Annotations = nginxIngressAnnotations,
                            },
                            Spec = new Extensionsv1beta1IngressSpec
                            {
                                Rules = rules,
                                Tls = tls,
                            },
                        };

                        await KubernetesClient.CreateNamespacedIngressAsync(ingress, DefaultNamespace);
                    },
                    swallowException: true));
        }

        /// <inheritdoc/>
        public Task RemoveBusyAgentFromDeploymentAsync(string agentName, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_register_agent",
                async (childLogger) =>
                {
                    childLogger.AddValue("AgentName", agentName);

                    var podList = await KubernetesClient.ListNamespacedPodAsync(DefaultNamespace, fieldSelector: $"metadata.name={agentName}");
                    var namedPod = podList.Items.SingleOrDefault();
                    if (namedPod == default)
                    {
                        throw new ArgumentException($"{nameof(agentName)} does not represent existing Pod in \"{DefaultNamespace}\" namespace.");
                    }

                    var updatedLabels = namedPod.Metadata.Labels == default ? new Dictionary<string, string>() : new Dictionary<string, string>(namedPod.Metadata.Labels);
                    updatedLabels.Remove(DeploymentNameLabelKey);
                    var patch = new JsonPatchDocument<V1Pod>();
                    patch.Replace(p => p.Metadata.Labels, updatedLabels);

                    await KubernetesClient.PatchNamespacedPodAsync(new V1Patch(patch), agentName, DefaultNamespace);
                });
        }

        /// <inheritdoc/>
        public Task KillAgentAsync(string agentName, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_kill_agent",
                async (childLogger) =>
                {
                    childLogger.AddValue("AgentName", agentName);

                    await KubernetesClient.DeleteNamespacedPodAsync(name: agentName, namespaceParameter: DefaultNamespace);
                });
        }

        /// <inheritdoc/>
        public Task<V1Service> WaitForServiceAvailableAsync(string serviceName, IDiagnosticsLogger logger)
        {
            return WaitForServiceAvailableAsync(serviceName, TimeSpan.FromSeconds(30), logger);
        }

        /// <inheritdoc/>
        public Task<V1Service> WaitForServiceAvailableAsync(string serviceName, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_register_wait_for_service_added",
                async (childLogger) =>
                {
                    childLogger.AddValue("ServiceName", serviceName);

                    var taskSource = new TaskCompletionSource<V1Service>();
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(timeout);

                    var cancellationRegistration = cts.Token.Register(() =>
                    {
                        taskSource.SetCanceled();
                    });

                    // ListNamespacedServiceWithHttpMessagesAsync ignores the cancellation token.
                    // Passing the cancellation token in case it gets fixed in future versions.
                    // https://github.com/kubernetes-client/csharp/issues/375
                    var service = await KubernetesClient.ListNamespacedServiceWithHttpMessagesAsync(
                        namespaceParameter: DefaultNamespace,
                        fieldSelector: $"metadata.name={serviceName}",
                        watch: true,
                        timeoutSeconds: Convert.ToInt32(timeout.TotalSeconds),
                        cancellationToken: cts.Token);

                    using (cts)
                    using (cancellationRegistration)
                    using (service.Watch<V1Service, V1ServiceList>((eventType, svc) =>
                    {
                        switch (eventType)
                        {
                            case WatchEventType.Added:
                            // We might need to modify the service in some cases instead of addding it.
                            case WatchEventType.Modified:
                                taskSource.SetResult(svc);
                                break;

                            default:
                                childLogger.AddValue("EventType", eventType.ToString());
                                childLogger.LogInfo($"service_watch_event");
                                break;
                        }
                    }))
                    {
                        return await taskSource.Task;
                    }
                });
        }

        /// <inheritdoc/>
        public Task<Extensionsv1beta1Ingress> WaitForIngressReadyAsync(string ingressName, IDiagnosticsLogger logger)
        {
            return WaitForIngressReadyAsync(ingressName, TimeSpan.FromSeconds(30), logger);
        }

        /// <inheritdoc/>
        public Task<Extensionsv1beta1Ingress> WaitForIngressReadyAsync(string ingressName, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_register_wait_for_ingress_ready",
                async (childLogger) =>
                {
                    childLogger.AddValue("IngressName", ingressName);

                    var taskSource = new TaskCompletionSource<Extensionsv1beta1Ingress>();
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(timeout);

                    var cancellationRegistration = cts.Token.Register(() =>
                    {
                        taskSource.SetCanceled();
                    });

                    // ListNamespacedServiceWithHttpMessagesAsync ignores the cancellation token.
                    // Passing the cancellation token in case it gets fixed in future versions.
                    // https://github.com/kubernetes-client/csharp/issues/375
                    var ingressHttpMessage = await KubernetesClient.ListNamespacedIngressWithHttpMessagesAsync(
                        namespaceParameter: DefaultNamespace,
                        fieldSelector: $"metadata.name={ingressName}",
                        watch: true,
                        timeoutSeconds: Convert.ToInt32(timeout.TotalSeconds),
                        cancellationToken: cts.Token);

                    using (cts)
                    using (cancellationRegistration)
                    using (ingressHttpMessage.Watch<Extensionsv1beta1Ingress, Extensionsv1beta1IngressList>((eventType, ing) =>
                    {
                        switch (eventType)
                        {
                            case WatchEventType.Added:
                            // We might need to modify the service in some cases instead of addding it.
                            case WatchEventType.Modified:
                                taskSource.SetResult(ing);
                                break;

                            default:
                                childLogger.AddValue("EventType", eventType.ToString());
                                childLogger.LogInfo($"ingress_watch_event");
                                break;
                        }
                    }))
                    {
                        return await taskSource.Task;
                    }
                });
        }

        /// <inheritdoc/>
        public Task<V1Endpoints> WaitForEndpointReadyAsync(string endpointName, IDiagnosticsLogger logger)
        {
            return WaitForEndpointReadyAsync(endpointName, TimeSpan.FromSeconds(30), logger);
        }

        /// <inheritdoc/>
        public Task<V1Endpoints> WaitForEndpointReadyAsync(string endpointName, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_register_wait_for_endpoint_ready",
                async (childLogger) =>
                {
                    childLogger.AddValue("EndpointName", endpointName);

                    var taskSource = new TaskCompletionSource<V1Endpoints>();
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(timeout);

                    var cancellationRegistration = cts.Token.Register(() =>
                    {
                        taskSource.SetCanceled();
                    });

                    // ListNamespacedServiceWithHttpMessagesAsync ignores the cancellation token.
                    // Passing the cancellation token in case it gets fixed in future versions.
                    // https://github.com/kubernetes-client/csharp/issues/375
                    var endpointHttpMessage = await KubernetesClient.ListNamespacedEndpointsWithHttpMessagesAsync(
                        namespaceParameter: DefaultNamespace,
                        fieldSelector: $"metadata.name={endpointName}",
                        watch: true,
                        timeoutSeconds: Convert.ToInt32(timeout.TotalSeconds),
                        cancellationToken: cts.Token);

                    using (cts)
                    using (cancellationRegistration)
                    using (endpointHttpMessage.Watch<V1Endpoints, V1EndpointsList>((eventType, endpoint) =>
                    {
                        switch (eventType)
                        {
                            case WatchEventType.Added:
                            // We might need to modify the endpoint in some cases instead of addding it.
                            case WatchEventType.Modified:
                                taskSource.SetResult(endpoint);
                                break;

                            default:
                                childLogger.AddValue("EventType", eventType.ToString());
                                childLogger.LogInfo($"endpoint_watch_event");
                                break;
                        }
                    }))
                    {
                        return await taskSource.Task;
                    }
                });
        }
    }
}
