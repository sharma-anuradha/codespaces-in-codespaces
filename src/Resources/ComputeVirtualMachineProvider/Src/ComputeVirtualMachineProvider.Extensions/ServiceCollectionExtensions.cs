// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;

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
            services.AddSingleton<IQueueProvider, VirtualMachineQueueProvider>();
            services.AddSingleton<IDeploymentManager, VirtualMachineDeploymentManager>();
            services.AddSingleton<ICreateVirtualMachineStrategy, CreateLinuxVirtualMachineBasicStrategy>();
            services.AddSingleton<ICreateVirtualMachineStrategy, CreateLinuxVirtualMachineWithNicStrategy>();
            services.AddSingleton<ICreateVirtualMachineStrategy, CreateWindowsVirtualMachineBasicStrategy>();
            services.AddSingleton<ICreateVirtualMachineStrategy, CreateWindowsVirtualMachineWithOSDiskStrategy>();
            services.AddSingleton<ICreateVirtualMachineStrategy, CreateWindowsVirtualMachineWithNicStrategy>();
            services.AddSingleton<ICreateVirtualMachineStrategy, CreateWindowsVirtualMachineWithNicAndOSDiskStrategy>();
            return services;
        }
    }
}
