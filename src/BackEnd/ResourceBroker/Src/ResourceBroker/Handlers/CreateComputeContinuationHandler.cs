// <copyright file="CreateComputeContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    public class CreateComputeContinuationHandler
        : CreateResourceContinuationHandler, ICreateComputeContinuationHandler
    {
        public CreateComputeContinuationHandler(
            IComputeProvider computeProvider,
            IResourceRepository resourceRepository,
            IAzureSubscriptionCatalog subscriptionCatalog,
            IMapper mapper)
            : base(resourceRepository, subscriptionCatalog, mapper)
        {
            ComputeProvider = computeProvider;
        }

        /// <inheritdoc/>
        protected override string TargetName => "JobCreateCompute";

        /// <inheritdoc/>
        protected override ResourceType TargetType => ResourceType.ComputeVM;

        private IComputeProvider ComputeProvider { get; }

        /// <inheritdoc/>
        protected async override Task<ResourceCreateContinuationResult> CreateResourceAsync(CreateResourceContinuationInput input, string continuationToken, IDiagnosticsLogger logger)
        {
            var didParseLocation = Enum.TryParse(input.Location, true, out AzureLocation azureLocation);
            if (!didParseLocation)
            {
                throw new NotSupportedException($"Provided location of '{input.Location}' is not supported.");
            }

            var providerInput = new VirtualMachineProviderCreateInput
            {
                AzureVmLocation = azureLocation,
                AzureSkuName = input.SkuName,
                AzureSubscription = Guid.Parse(input.Subscription),
                AzureResourceGroup = input.ResourceGroup,
                AzureVirtualMachineImage = "Canonical:UbuntuServer:18.04-LTS:latest",
            };

            return await ComputeProvider.CreateAsync(providerInput, continuationToken);
        }
    }
}
