// <copyright file="DeleteOneAzureResourceJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Perform the work of deleting one Azure resource
    /// </summary>
    public class DeleteOneAzureResourceJobHandler : JobHandlerPayloadBase<DeleteOneAzureResourcePayload>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOneAzureResourceJobHandler"/> class.
        /// </summary>
        /// <param name="azureClientFactory">Azure client factory</param>
        public DeleteOneAzureResourceJobHandler(
            IAzureClientFactory azureClientFactory)
        {
            AzureClientFactory = azureClientFactory;
        }

        private IAzureClientFactory AzureClientFactory { get; }    

        private string LogBaseName { get; } = ResourceLoggingConstants.DeleteOneAzureResourceJobHandler;

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(DeleteOneAzureResourcePayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_delete",
                async (childLogger) =>
                {
                    childLogger.AddBaseAzureResource(payload.AzureResource);

                    var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(payload.SubscriptionId));
                    var apiVersion = await GetApiVersionForResourceTypeAsync(azure, payload.AzureResource.Type);
                    await azure.Resources.BeginDeleteByIdAsync(payload.AzureResource.Id, apiVersion);  
                },
                swallowException: true);
        }

        private async Task<string> GetApiVersionForResourceTypeAsync(IResourceManagementClient azure, string resourceType)
        {
            // Format is "providerName/kind" - e.g. Microsoft.Compute/virtualMachines
            var parts = resourceType.Split('/');
            if (parts.Length != 2)
            {
                throw new FormatException($"Azure resource type in unexpected format: {resourceType}");
            }

            var providerName = parts[0];

            var provider = await azure.Providers.GetAsync(providerName);
            if (provider == null)
            {
                throw new NotFoundException($"Azure Provider not found: {providerName}");
            }

            var apiVersionsForTypes = provider.ResourceTypes
                .Select((type) => ($"{providerName}/{type.ResourceType}", type.ApiVersions.OrderByDescending(v => v).FirstOrDefault()))
                .Where((pair) => !string.IsNullOrEmpty(pair.Item2));

            foreach (var (type, version) in apiVersionsForTypes)
            {
                if (type == resourceType)
                {
                    return version;
                }
            }

            // Provider was found, but couldn't find this specific type in it
            throw new NotFoundException($"Azure resource type not found in Provider: {resourceType}");
        }
    }
}
