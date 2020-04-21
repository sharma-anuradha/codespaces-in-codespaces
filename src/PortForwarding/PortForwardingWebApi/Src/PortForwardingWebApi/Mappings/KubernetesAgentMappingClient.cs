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
                    childLogger.AddValue("pod_name", registration.Name);
                    childLogger.AddValue("pod_uid", registration.Uid);

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

            logger
                .FluentAddBaseValue("workspace_id", mapping.WorkspaceId)
                .FluentAddBaseValue("source_port", mapping.SourcePort)
                .FluentAddBaseValue("pod_name", mapping.AgentName)
                .FluentAddBaseValue("pod_uid", mapping.AgentUid)
                .FluentAddBaseValue("destination_port", mapping.DestinationPort);

            await logger.OperationScopeAsync(
                "kubernetes_create_agent_service",
                async (childLogger) =>
                {
                    childLogger.AddValue("kubernetes_service_name", mapping.GetKubernetesServiceName());

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
                            Selector = new Dictionary<string, string>
                            {
                                [NameLabelKey] = mapping.AgentName,
                                [UidLabelKey] = mapping.AgentUid,
                            },
                            Ports = new List<V1ServicePort>
                            {
                                new V1ServicePort { Port = MappingServicePort, TargetPort = mapping.DestinationPort },
                            },
                        },
                    };

                    await KubernetesClient.CreateNamespacedServiceAsync(service, DefaultNamespace);
                });

            await logger.OperationScopeAsync(
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
                                        Backend = new Extensionsv1beta1IngressBackend(mapping.GetKubernetesServiceName(), MappingServicePort),
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
                });
        }

        /// <inheritdoc/>
        public Task RemoveBusyAgentFromDeploymentAsync(string agentName, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_register_agent",
                async (childLogger) =>
                {
                    childLogger.AddValue("pod_name", agentName);

                    var podList = await KubernetesClient.ListNamespacedPodAsync(DefaultNamespace, fieldSelector: $"metadata.name={agentName}");
                    var namedPod = podList.Items.SingleOrDefault();
                    if (namedPod == default)
                    {
                        throw new ArgumentException($"{nameof(agentName)} does not represent existing Pod in \"{DefaultNamespace}\" namespace.");
                    }

                    var updatedLabels = new Dictionary<string, string>(namedPod.Metadata.Labels);
                    updatedLabels.Remove(DeploymentNameLabelKey);
                    var patch = new JsonPatchDocument<V1Pod>();
                    patch.Replace(p => p.Metadata.Labels, updatedLabels);

                    await KubernetesClient.PatchNamespacedPodAsync(new V1Patch(patch), agentName, DefaultNamespace);
                });
        }

        /// <inheritdoc/>
        public Task WaitForServiceAvailableAsync(string serviceName, IDiagnosticsLogger logger)
        {
            return WaitForServiceAvailableAsync(serviceName, TimeSpan.FromSeconds(30), logger);
        }

        /// <inheritdoc/>
        public Task WaitForServiceAvailableAsync(string serviceName, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "kubernetes_register_wait_for_service_added",
                async (childLogger) =>
                {
                    childLogger.AddValue("service_name", serviceName);

                    var taskSource = new TaskCompletionSource<bool>();
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
                    using (service.Watch<V1Service, V1ServiceList>((eventType, pod) =>
                    {
                        switch (eventType)
                        {
                            case WatchEventType.Added:
                            // We might need to modify the service in some cases instead of addding it.
                            case WatchEventType.Modified:
                                taskSource.SetResult(true);
                                break;

                            default:
                                childLogger.AddValue("event_type", eventType.ToString());
                                childLogger.LogInfo($"service_watch_event");
                                break;
                        }
                    }))
                    {
                        await taskSource.Task;
                    }
                });
        }
    }
}
