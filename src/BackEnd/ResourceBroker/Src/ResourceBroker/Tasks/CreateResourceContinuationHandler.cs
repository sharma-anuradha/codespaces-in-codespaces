// <copyright file="CreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    public abstract class CreateResourceContinuationHandler : IContinuationTaskMessageHandler
    {
        public CreateResourceContinuationHandler(
            IResourceRepository resourceRepository,
            IAzureSubscriptionCatalog subscriptionCatalog,
            IMapper mapper)
        {
            ResourceRepository = resourceRepository;
            SubscriptionCatalog = subscriptionCatalog;
            Mapper = mapper;
        }

        protected abstract string TargetName { get; }

        protected abstract ResourceType TargetType { get; }

        private IResourceRepository ResourceRepository { get; }

        private IAzureSubscriptionCatalog SubscriptionCatalog { get; }

        private IMapper Mapper { get; }

        public virtual bool CanHandle(ResourceJobQueuePayload payload)
        {
            return payload.Target == TargetName;
        }

        /// <inheritdoc/>
        public async Task<ContinuationTaskMessageHandlerResult> Continue(ContinuationTaskMessageHandlerInput input, IDiagnosticsLogger logger, string continuationToken)
        {
            var handlerInput = Mapper.Map<CreateResourceContinuationInput>(input.HandlerInput);

            return await Continue(handlerInput, logger, continuationToken);
        }

        protected virtual async Task<ContinuationTaskMessageHandlerResult> Continue(
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            logger.FluentAddValue("IsInitialContinuation", string.IsNullOrEmpty(continuationToken).ToString());

            // First time through we want to add the resource
            if (continuationToken == null)
            {
                // Add record to database
                await CreateResourceRecord(input, logger);
            }
            else
            {
                // Update record in database
                await UpdateResourceRecord(input, logger);
            }

            // Trigger core continuation
            var result = await CreateResourceAsync(input, continuationToken);

            // If we are finished, update the db record to reflect that
            if (string.IsNullOrEmpty(result.ContinuationToken))
            {
                logger
                    .FluentAddValue("IsFinalContinuation", true.ToString())
                    .FluentAddValue("DidStatusUpdate", "true");

                await FinalizeResourceRecord(input, logger);
            }

            return new ContinuationTaskMessageHandlerResult
            {
                HandlerResult = result,
                Metadata = null,
            };
        }

        protected abstract Task<BaseContinuationResult> CreateResourceAsync(CreateResourceContinuationInput input, string continuationToken);

        private async Task<ResourceRecord> CreateResourceRecord(
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            // Common properties
            var id = Guid.NewGuid().ToString();
            var time = DateTime.UtcNow;

            // Core recrod
            var record = new ResourceRecord
            {
                Id = id,
                Type = TargetType,
                IsReady = false,
                Ready = null,
                IsAssigned = false,
                Assigned = null,
                Created = time,
                Location = input.Location,
                SkuName = input.SkuName,
                ResourceGroup = $"RG-{id}",
                Subscription = SubscriptionCatalog.AzureSubscriptions.FirstOrDefault().SubscriptionId,
            };
            record.UpdateProvisioningStatus(ResourceProvisioningStatus.Queued);

            // Update input
            input.InstanceId = record.Id;
            input.ResourceGroup = record.ResourceGroup;
            input.Subscription = record.Subscription;

            // Create the actual record
            await ResourceRepository.CreateAsync(record, logger);

            return record;
        }

        private async Task<ResourceRecord> UpdateResourceRecord(
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            // If we where in Provisioning state, update it to be so
            var record = await ResourceRepository.GetAsync(input.InstanceId, logger);
            if (record.ProvisioningStatus != ResourceProvisioningStatus.Provisioning)
            {
                logger.FluentAddValue("DidStatusUpdate", "true");

                record.UpdateProvisioningStatus(ResourceProvisioningStatus.Provisioning);

                await ResourceRepository.UpdateAsync(record, logger);
            }

            return record;
        }

        private async Task<ResourceRecord> FinalizeResourceRecord(
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            var record = await ResourceRepository.GetAsync(input.InstanceId, logger);

            record.IsReady = true;
            record.Ready = DateTime.UtcNow;
            record.UpdateProvisioningStatus(ResourceProvisioningStatus.Completed);

            await ResourceRepository.UpdateAsync(record, logger);

            return record;
        }
    }
}
