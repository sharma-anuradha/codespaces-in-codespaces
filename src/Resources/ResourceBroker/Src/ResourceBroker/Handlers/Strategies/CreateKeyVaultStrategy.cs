// <copyright file="CreateKeyVaultStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Create resource basic strategy.
    /// </summary>
    public class CreateKeyVaultStrategy : ICreateResourceStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateKeyVaultStrategy"/> class.
        /// </summary>
        /// <param name="keyVaultProvider">Keyvault provider.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        public CreateKeyVaultStrategy(
            IKeyVaultProvider keyVaultProvider,
            ICapacityManager capacityManager)
        {
            KeyVaultProvider = Requires.NotNull(keyVaultProvider, nameof(keyVaultProvider));
            CapacityManager = Requires.NotNull(capacityManager, nameof(capacityManager));
        }

        private IKeyVaultProvider KeyVaultProvider { get; }

        private ICapacityManager CapacityManager { get; }

        /// <inheritdoc/>
        public bool CanHandle(CreateResourceContinuationInput input)
        {
            return input.Type == ResourceType.KeyVault;
        }

        /// <inheritdoc/>
        public async Task<ContinuationInput> BuildCreateOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            // Base resource tags that will be attached
            var resourceTags = resource.Value.GetResourceTags(input.Reason);
            ContinuationInput result;

            if (resource.Value.Type == ResourceType.KeyVault)
            {
                if (input.ResourcePoolDetails is ResourcePoolKeyVaultDetails keyVaultDetails)
                {
                    // Set up the selection criteria and select a subscription/location.
                    var criteria = new List<AzureResourceCriterion>
                    {
                        new AzureResourceCriterion { ServiceType = ServiceType.KeyVault, Quota = ServiceType.KeyVault.ToString(), Required = 1 },
                    };

                    var resourceLocation = await CapacityManager.SelectAzureResourceLocation(
                        criteria, keyVaultDetails.Location, logger.NewChildLogger());

                    result = new KeyVaultProviderCreateInput
                    {
                        ResourceId = resource.Value.Id,
                        AzureLocation = keyVaultDetails.Location,
                        AzureSkuName = keyVaultDetails.SkuName,
                        AzureSubscriptionId = resourceLocation.Subscription.SubscriptionId,
                        AzureTenantId = resourceLocation.Subscription.ServicePrincipal.TenantId,
                        AzureObjectId = resourceLocation.Subscription.ServicePrincipal.ObjectId,
                        AzureResourceGroup = resourceLocation.ResourceGroup,
                        ResourceTags = resourceTags,
                    };
                }
                else
                {
                    throw new NotSupportedException($"Pool keyvault details type is not selected - {input.ResourcePoolDetails.GetType()}");
                }
            }
            else
            {
                throw new NotSupportedException($"Resource type is not supported - {resource.Value.Type}");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            ResourceCreateContinuationResult result;

            // Run create operation
            if (resource.Value.Type == ResourceType.KeyVault)
            {
                result = await KeyVaultProvider.CreateAsync((KeyVaultProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            return result;
        }
    }
}