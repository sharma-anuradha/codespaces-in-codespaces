// <copyright file="CreateComputeStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Strategies
{
    /// <summary>
    /// Create compute.
    /// </summary>
    public class CreateComputeStrategy : ICreateComponentStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateComputeStrategy"/> class.
        /// </summary>
        /// <param name="computeProvider">Target compute provider.</param>
        /// <param name="controlPlaneInfo">the control plane info.</param>
        /// <param name="tokenProvider">Token provider.</param>
        /// <param name="imageUrlGenerator">Image URL generator.</param>
        public CreateComputeStrategy(
            IComputeProvider computeProvider,
            IControlPlaneInfo controlPlaneInfo,
            ITokenProvider tokenProvider,
            IImageUrlGenerator imageUrlGenerator)
        {
            ComputeProvider = Requires.NotNull(computeProvider, nameof(computeProvider));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            TokenProvider = Requires.NotNull(tokenProvider, nameof(tokenProvider));
            ImageUrlGenerator = Requires.NotNull(imageUrlGenerator, nameof(imageUrlGenerator));
        }

        private IComputeProvider ComputeProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ITokenProvider TokenProvider { get; }

        private IImageUrlGenerator ImageUrlGenerator { get; }

        /// <inheritdoc/>
        public async Task<ContinuationInput> BuildCreateOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var resourceTags = resource.Value.GetResourceTags(input.Reason);
            if (!(input.ResourcePoolDetails is ResourcePoolComputeDetails computeDetails))
            {
                throw new NotSupportedException($"Pool compute details type is not selected - {input.ResourcePoolDetails.GetType()}");
            }

            var operationInput = (CreateResourceWithComponentInput)input.OperationInput;

            // Get VM Agent Blob Url
            var token = await TokenProvider.GenerateVmTokenAsync(resource.Value.Id, logger);
            var url = await ImageUrlGenerator.ReadOnlyUrlByImageName(input.ResourcePoolDetails.Location, resource.Value.Type, computeDetails.VmAgentImageName);

            // Add additional tags
            resourceTags.Add(ResourceTagName.ComputeOS, computeDetails.OS.ToString());

            var result = new VirtualMachineProviderCreateInput
            {
                ResourceId = resource.Value.Id,
                VMToken = token,
                AzureVmLocation = computeDetails.Location,
                AzureSkuName = computeDetails.SkuName,
                AzureSubscription = operationInput.AzureSubscription,
                AzureResourceGroup = operationInput.AzureResourceGroup,
                AzureVirtualMachineImage = computeDetails.ImageName,
                VmAgentBlobUrl = url,
                ResourceTags = resourceTags,
                ComputeOS = computeDetails.OS,
                FrontDnsHostName = ControlPlaneInfo.Stamp.DnsHostName,
                CustomComponents = operationInput.CustomComponents,
            };

            return result;
        }

        /// <inheritdoc/>
        public bool CanHandle(CreateResourceContinuationInput input)
        {
            return input.Type == ResourceType.ComputeVM;
        }

        /// <inheritdoc/>
        public async Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            // Run create operation
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                return await ComputeProvider.CreateAsync((VirtualMachineProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }
        }
    }
}
