// <copyright file="ListDevStamps.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Cleans up devstamp.
    /// </summary>
    [Verb("listdevstamps", HelpText = "lists dev stamp resources.")]
    public class ListDevStamps : CommandBase
    {
        private const string CloudEnvironmentsPrefix = "cloud-environments-";

        /// <summary>
        /// Gets or sets a value indicating whether to enable bare output.
        /// </summary>
        [Option('b', "bare", HelpText = "prints bare output if set.")]
        public bool BareOutput { get; set; } = false;

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteAsync(services, stdout, stderr).Wait();
        }

        private async Task ExecuteAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var aliasStamps = new ConcurrentBag<string>();

            var azureSubscriptionCatalog = services.GetRequiredService<IAzureSubscriptionCatalog>();
            var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();

            var controlPlaneAzureResourceAcccessor = GetServiceProvider().GetRequiredService<IControlPlaneAzureResourceAccessor>();

            var (hostUrl, authKey) = await controlPlaneAzureResourceAcccessor.GetStampCosmosDbAccountAsync();
            using (var cosmosClient = new CosmosClient(
                    hostUrl,
                    authKey,
                    new CosmosClientOptions()
                    {
                        ApplicationRegion = Regions.WestUS2,
                    }))
            {
                var iterator = cosmosClient.GetDatabaseQueryIterator<DatabaseProperties>();
                do
                {
                    foreach (DatabaseProperties db in await iterator.ReadNextAsync())
                    {
                        var name = db.Id;
                        if (name.StartsWith(CloudEnvironmentsPrefix))
                        {
                            var alias = name.Replace(CloudEnvironmentsPrefix, string.Empty);
                            aliasStamps.Add(alias);

                            if (!this.BareOutput)
                            {
                                await stdout.WriteLineAsync($"Found dev stamp database {name}");
                            }
                        }
                    }
                }
                while (iterator.HasMoreResults);
            }

            foreach (var catalog in azureSubscriptionCatalog.AzureSubscriptions)
            {
                var subscriptionId = catalog.SubscriptionId;

                var azure = await azureClientFactory.GetAzureClientAsync(Guid.Parse(subscriptionId));
                foreach (var resourceGroup in await azure.ResourceGroups.ListAsync())
                {
                    if (resourceGroup.Name.EndsWith($"-{ResourceNameBuilder.ResourceGroupPostFix}"))
                    {
                        var alias = resourceGroup.Name.Replace($"-{ResourceNameBuilder.ResourceGroupPostFix}", string.Empty);
                        aliasStamps.Add(alias);

                        if (!this.BareOutput)
                        {
                            await stdout.WriteLineAsync($"Found dev stamp resource group {resourceGroup.Name} in subscription {subscriptionId}");
                        }
                    }
                }
            }

            if (this.BareOutput)
            {
                foreach (var item in aliasStamps.ToArray().Distinct())
                {
                    await stdout.WriteLineAsync(item);
                }
            }
        }
    }
}
