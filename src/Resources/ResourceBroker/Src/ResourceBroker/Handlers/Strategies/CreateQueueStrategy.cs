// <copyright file="CreateQueueStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Strategies
{
    /// <summary>
    /// Create queue strategy.
    /// </summary>
    public class CreateQueueStrategy : ICreateComponentStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueStrategy"/> class.
        /// </summary>
        /// <param name="queueProvider">Queue provider.</param>
        public CreateQueueStrategy(IQueueProvider queueProvider)
        {
            QueueProvider = Requires.NotNull(queueProvider, nameof(queueProvider));
        }

        private IQueueProvider QueueProvider { get; }

        /// <inheritdoc/>
        public Task<ContinuationInput> BuildCreateOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            if (!(input.ResourcePoolDetails is ResourcePoolComputeDetails computeDetails))
            {
                throw new NotSupportedException($"Pool compute details type is not selected - {input.ResourcePoolDetails.GetType()}");
            }

            var result = new QueueProviderCreateInput
            {
                AzureLocation = input.ResourcePoolDetails.Location,
                QueueName = $"{Guid.NewGuid()}-input-queue",
            };

            return Task.FromResult<ContinuationInput>(result);
        }

        /// <inheritdoc/>
        public bool CanHandle(CreateResourceContinuationInput input)
        {
            return input.Type == ResourceType.InputQueue;
        }

        /// <inheritdoc/>
        public async Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            // Run create operation
            if (input.Type == ResourceType.InputQueue)
            {
                return await QueueProvider.CreateAsync((QueueProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }
        }
    }
}
