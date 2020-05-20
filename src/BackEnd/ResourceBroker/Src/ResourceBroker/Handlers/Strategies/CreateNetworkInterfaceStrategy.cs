// <copyright file="CreateNetworkInterfaceStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeNetworkInterfaceProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Strategies
{
    /// <summary>
    /// Create compute.
    /// </summary>
    public class CreateNetworkInterfaceStrategy : ICreateComponentStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateNetworkInterfaceStrategy"/> class.
        /// </summary>
        /// <param name="networkInterfaceProvider">Network Interface provider.</param>
        public CreateNetworkInterfaceStrategy(INetworkInterfaceProvider networkInterfaceProvider)
        {
            NetworkInterfaceProvider = networkInterfaceProvider;
        }

        private INetworkInterfaceProvider NetworkInterfaceProvider { get; }

        /// <inheritdoc/>
        public Task<ContinuationInput> BuildCreateOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var resourceTags = resource.Value.GetResourceTags(input.Reason);

            if (!(input.ResourcePoolDetails is ResourcePoolComputeDetails computeDetails))
            {
                throw new NotSupportedException($"Pool compute details type is not selected - {input.ResourcePoolDetails.GetType()}");
            }

            if (!(input.Options is CreateComputeContinuationInputOptions computeOption))
            {
                throw new NotSupportedException($"Pool compute options is not provided - {input.Options?.GetType()}");
            }

            var subnetResourceId = Requires.NotNull(computeOption.SubnetResourceId, nameof(computeOption.SubnetResourceId));
            var subnetSubscription = ResourceId.FromString(subnetResourceId)?.SubscriptionId;
            var subnetResourceGroup = ResourceId.FromString(subnetResourceId)?.ResourceGroupName;
            if (string.IsNullOrEmpty(subnetSubscription))
            {
                throw new NotSupportedException($"Subnet resource id is not valid - {subnetResourceId}");
            }

            // TODO:: Check for network interface capacity in subnetSubscription
            var result = new NetworkInterfaceProviderCreateInput
            {
                SubnetAzureResourceId = subnetResourceId,
                SubnetSubscription = Guid.Parse(subnetSubscription),
                ResourceGroup = subnetResourceGroup,
                Location = computeDetails.Location,
                ResourceTags = resourceTags,
            };

            return Task.FromResult<ContinuationInput>(result);
        }

        /// <inheritdoc/>
        public bool CanHandle(CreateResourceContinuationInput input)
        {
            return input.Type == ResourceType.NetworkInterface;
        }

        /// <inheritdoc/>
        public async Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            // Run create operation
            if (input.Type == ResourceType.NetworkInterface)
            {
                return await NetworkInterfaceProvider.CreateAsync((NetworkInterfaceProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }
        }
    }
}
