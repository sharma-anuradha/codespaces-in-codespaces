﻿// <copyright file="CreateNetworkInterfaceStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeNetworkInterfaceProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

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
        /// <param name="diskProvider">Disk provider.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="tokenProvider">Token provider.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="creationStrategies">Resource creation strategies.</param>
        public CreateNetworkInterfaceStrategy(
            INetworkInterfaceProvider networkInterfaceProvider)
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

            var subnetResourceInfo = Requires.NotNull(computeOption.SubnetResourceInfo, nameof(computeOption.SubnetResourceInfo));

            var operationInput = (CreateResourceWithComponentInput)input.OperationInput;

            // Add additional tags
            resourceTags.Add(ResourceTagName.ComputeOS, computeDetails.OS.ToString());

            var result = new NetworkInterfaceProviderCreateInput
            {
                VnetName = subnetResourceInfo.VnetName,
                SubnetName = subnetResourceInfo.SubnetName,
                ResourceGroup = operationInput.AzureResourceGroup,
                Location = computeDetails.Location,
                Subscription = subnetResourceInfo.SubscriptionId,
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
            if (resource.Value.Type == ResourceType.NetworkInterface)
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
