// <copyright file="AzureCosmosManagementHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Helpers for the Azure Management Fluent for Cosmos.
    /// </summary>
    internal static class AzureCosmosManagementHelpers
    {
        public static bool IsAzureCosmosConnectionDefined(
            this ApplicationServicePrincipal applicationServicePrincipal,
            AppSettingsBase appSettings)
        {
            Requires.NotNull(appSettings, nameof(appSettings));

            return applicationServicePrincipal.IsDefined() &&
                !string.IsNullOrEmpty(appSettings.SubscriptionId) &&
                !string.IsNullOrEmpty(appSettings.ResourceGroupInstanceName) &&
                !string.IsNullOrEmpty(appSettings.AzureCosmosDbName);
        }

        public static async Task<(string, string)> GetAzureCosmosConnection(
            this ApplicationServicePrincipal applicationServicePrincipal,
            ILogger logger,
            AppSettingsBase appSettings,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(applicationServicePrincipal, nameof(applicationServicePrincipal));
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(appSettings, nameof(appSettings));

            logger.LogInformation($"Get Azure cosmos database connection name:{appSettings.AzureCosmosDbName} resource group:{appSettings.ResourceGroupInstanceName} subscriptionId:{appSettings.SubscriptionId}");

            var cosmosDBManager = CosmosDBManager.Authenticate(applicationServicePrincipal.GetAzureCredentials(), appSettings.SubscriptionId);
            var cosmosDbAccount = await cosmosDBManager.CosmosDBAccounts.GetByResourceGroupAsync(appSettings.ResourceGroupInstanceName, appSettings.AzureCosmosDbName, cancellationToken);

            if (cosmosDbAccount == null)
            {
                throw new Exception($"Cosmos DB name:{appSettings.AzureCosmosDbName} not found on resource group:{appSettings.ResourceGroupInstanceName} subscriptionId:{appSettings.SubscriptionId}");
            }

            var endpoint = cosmosDbAccount.DocumentEndpoint;
            var keys = await cosmosDbAccount.ListKeysAsync();
            var key = keys.PrimaryMasterKey;
            return (endpoint, key);
        }
    }
}
