// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Jobs;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the Token creation.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds VM token provider and dependencies to the collection of services.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>Collection of services along with vm token provider.</returns>
        public static IServiceCollection AddVMTokenProvider(
            this IServiceCollection services)
        {
            Requires.NotNull(services, nameof(services));

            return services
                .AddCommonServices()
                .AddSingleton<IVirtualMachineTokenProvider, VirtualMachineTokenProvider>();
        }

        /// <summary>
        /// Adds VM token validator and dependencies to the collection of services.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>Collection of services along with vm token validator.</returns>
        public static IServiceCollection AddVMTokenValidator(
            this IServiceCollection services)
        {
            Requires.NotNull(services, nameof(services));

            return services
                .AddCommonServices()
                .AddSingleton<IVirtualMachineTokenValidator, VirtualMachineTokenValidator>();
        }

        private static IServiceCollection AddCommonServices(
            this IServiceCollection services)
        {
            return services
                .AddSingleton<ICertificateProvider, CertificateProvider>()
                .AddSingleton<IAsyncWarmup, InitializeCertificateProvider>();
        }
    }
}
