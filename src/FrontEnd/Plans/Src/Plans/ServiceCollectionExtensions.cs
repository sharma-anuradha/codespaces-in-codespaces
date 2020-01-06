// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Plan Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="PlanRepository"/> and <see cref="IPlanManager"/> to the service collection.
        /// </summary>
        /// <param name="services">The IServiceCollection object.</param>
        /// <param name="planManagerSettings">The PlanManagerSettings object.</param>
        /// <param name="useMockPlanRepository">Boolean to indicate if mocks should be used.</param>
        /// <returns>An IServiceCollection.</returns>
        public static IServiceCollection AddPlanManager(
            this IServiceCollection services,
            PlanManagerSettings planManagerSettings,
            bool useMockPlanRepository)
        {
            services.AddSingleton(planManagerSettings);

            if (useMockPlanRepository)
            {
                services.AddSingleton<IPlanRepository, MockPlanRepository>();
            }
            else
            {
                services.AddDocumentDbCollection<VsoPlan, IPlanRepository, PlanRepository>(PlanRepository.ConfigureOptions);
            }

            // The Plan mangaer
            services.AddSingleton<IPlanManager, PlanManager>();

            return services;
        }

        /// <summary>
        /// Add the <see cref="PlanWorker"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>service instance.</returns>
        public static IServiceCollection AddPlanWorker(
            this IServiceCollection services)
        {
            // The Plan worker
            services.AddHostedService<PlanWorker>();

            return services;
        }
    }
}
