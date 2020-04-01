// <copyright file="ServiceCollectionPcfAgentExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent
{
    /// <summary>
    /// ServiceCollection PCF Agent Extensions.
    /// </summary>
    public static class ServiceCollectionPcfAgentExtensions
    {
        /// <summary>
        /// Service collection extension to enable privacy command feed processing.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="pcfAgentId">PCF Agent Id.</param>
        /// <param name="useMocksForLocalDevelopment">Use mocks for developemnt.</param>
        /// <returns>Updated service collection.</returns>
        public static IServiceCollection AddPcfAgent(this IServiceCollection services, Guid pcfAgentId, bool useMocksForLocalDevelopment)
        {
            services.AddHostedService<PcfAgentWorker>();
            services.AddSingleton<IPrivacyDataManager, PrivacyDataManager>();
            services.AddSingleton<IPrivacyDataAgent, DataAgent>();
            services.AddSingleton<IContinuationTaskMessageHandler, EnvironmentDeletionContinuationHandler>();
            services.AddTransient<CommandFeedLogger, DiagnosticsCommandFeedLogger>();

            if (useMocksForLocalDevelopment)
            {
                services.AddSingleton<ICommandFeedClient, RandomMockCommandFeedClient>();
            }
            else
            {
                services.AddSingleton<ICommandFeedClient, CommandFeedClient>(serviceProvider =>
                {
                    var servicePrincipal = serviceProvider.GetRequiredService<IServicePrincipal>();

                    return new CommandFeedClient(
                        agentId: pcfAgentId,
                        aadClientId: servicePrincipal.ClientId,
                        aadClientSecret: servicePrincipal.GetClientSecretAsync().Result,
                        logger: serviceProvider.GetService<CommandFeedLogger>());
                });
            }

            return services;
        }
    }
}
