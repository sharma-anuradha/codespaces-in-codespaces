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
        ///
        /// </summary>
        /// <param name="services"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static IServiceCollection AddComputeVirtualMachineProvider(
            this IServiceCollection services,
            AppSettings appSettings)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(appSettings, nameof(appSettings));

            // Short circuit things if Resource Providers is being mocked
            if (appSettings.UseMocksForResourceProviders)
            {
                return services;
            }

            // Core services
            services.AddSingleton<IComputeProvider, VirtualMachineProvider>();
            services.AddSingleton<IDeploymentManager, LinuxVirtualMachineManager>();

            // External Service
            if (appSettings.UseMocksForExternalDependencies)
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
