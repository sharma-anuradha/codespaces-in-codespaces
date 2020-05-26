// <copyright file="CreateComputeStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models;
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
        /// <param name="queueProvider">Queue provider.</param>
        public CreateComputeStrategy(
            IComputeProvider computeProvider,
            IControlPlaneInfo controlPlaneInfo,
            ITokenProvider tokenProvider,
            IImageUrlGenerator imageUrlGenerator,
            IQueueProvider queueProvider)
        {
            ComputeProvider = Requires.NotNull(computeProvider, nameof(computeProvider));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            TokenProvider = Requires.NotNull(tokenProvider, nameof(tokenProvider));
            ImageUrlGenerator = Requires.NotNull(imageUrlGenerator, nameof(imageUrlGenerator));
            QueueProvider = Requires.NotNull(queueProvider, nameof(queueProvider));
        }

        private IComputeProvider ComputeProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ITokenProvider TokenProvider { get; }

        private IImageUrlGenerator ImageUrlGenerator { get; }

        private IQueueProvider QueueProvider { get; }

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

            // Get queue connection information.
            var inputQueueComponent = operationInput.CustomComponents.Single(x => x.ComponentType == ResourceType.InputQueue);
            var queueConnectionInfo = await QueueProvider.GetQueueConnectionInfoAsync(inputQueueComponent.AzureResourceInfo, logger.NewChildLogger());

            // Add additional tags
            resourceTags.Add(ResourceTagName.ComputeOS, computeDetails.OS.ToString());

            // Set options
            var options = default(VirtualMachineProviderCreateOptions);
            if (input.Options is CreateComputeContinuationInputOptions computeOptions)
            {
                options = new VirtualMachineResumeOptions()
                {
                    HardBoot = computeOptions.HardBoot,
                };
            }

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
                QueueConnectionInfo = queueConnectionInfo,
                Options = options,
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
            var result = default(ResourceCreateContinuationResult);

            // Run create operation
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                result = await ComputeProvider.CreateAsync((VirtualMachineProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            return result;
        }
    }
}
