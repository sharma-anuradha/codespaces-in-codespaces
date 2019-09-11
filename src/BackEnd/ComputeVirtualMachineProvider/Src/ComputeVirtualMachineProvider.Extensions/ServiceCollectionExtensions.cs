// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// <see cref="IServiceCollection"/> extensions for the compute-virtual-machine provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddComputeVirtualMachineProvider(
            this IServiceCollection services,
            MocksSettings mocksSettings = null)
        {
            Requires.NotNull(services, nameof(services));

            // Short circuit things if Resource Providers is being mocked
            if (mocksSettings?.UseMocksForResourceProviders == true)
            {
                return services;
            }

            // Core services
            services.AddSingleton<IComputeProvider, VirtualMachineProvider>();
            services.AddSingleton<IDeploymentManager, LinuxVirtualMachineManager>();
            services.AddSingleton<IDeploymentManager, WindowsVirtualMachineManager>();

            // External Service
            if (mocksSettings?.UseMocksForExternalDependencies == true)
            {
                // TODO: this needs to be updated
                var clientFactoryMock = new AzureClientFactoryMock("{what_should_this_be}");
                services.AddSingleton<IAzureClientFactory>(clientFactoryMock);
            }
            else
            {
                services.AddSingleton<IAzureClientFactory, AzureClientFactory>();
            }

            return services;
        }
    }
}
