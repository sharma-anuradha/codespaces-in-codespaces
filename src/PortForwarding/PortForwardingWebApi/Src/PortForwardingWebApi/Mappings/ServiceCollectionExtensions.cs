// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for ConnectionsManager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="KubernetesAgentMappingClient"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="appSettings">The service settings.</param>
        /// <param name="isRunningInAzure">The flag to know if service is running in azure.</param>
        /// <returns>service instance.</returns>
        public static IServiceCollection AddAgentMappingClient(
            this IServiceCollection services,
            PortForwardingAppSettings appSettings,
            bool isRunningInAzure)
        {
            if (appSettings.UseMockKubernetesMappingClientInDevelopment)
            {
                services.AddSingleton<IAgentMappingClient, NullAgentMappingClient>();
                return services;
            }

            // Based on whether running in a cluster, use config from either the cluster or the config file used by kubectl.
            var config = isRunningInAzure
                ? KubernetesClientConfiguration.InClusterConfig()
                : KubernetesClientConfiguration.BuildConfigFromConfigFile();

            var client = new Kubernetes(config);

            services.AddSingleton<IKubernetes>(client);
            services.AddSingleton<IAgentMappingClient, KubernetesAgentMappingClient>();

            return services;
        }
    }
}
