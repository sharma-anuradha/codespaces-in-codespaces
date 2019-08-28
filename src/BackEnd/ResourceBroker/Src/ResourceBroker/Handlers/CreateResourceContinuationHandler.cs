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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CreateResourceContinuationHandler : IContinuationTaskMessageHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="resourceRepository"></param>
        /// <param name="subscriptionCatalog"></param>
        /// <param name="mapper"></param>
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

        /// <inheritdoc/>
        public virtual bool CanHandle(ResourceJobQueuePayload payload)
        {
            return payload.Target == TargetName;
        }

        /// <inheritdoc/>
        public async Task<ContinuationTaskMessageHandlerResult> Continue(
            ContinuationTaskMessageHandlerInput handlerInput,
            IDiagnosticsLogger logger)
        {
            var input = Mapper.Map<CreateResourceContinuationInput>(handlerInput.Input);

            return await Continue(input, handlerInput.ContinuationToken, logger);
        }

        private async Task<ContinuationTaskMessageHandlerResult> Continue(
            CreateResourceContinuationInput input,
            string continuationToken,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("ContinuationIsInitial", string.IsNullOrEmpty(continuationToken).ToString());

            // First time through we want to add the resource
            var record = default(ResourceRecord);
            if (continuationToken == null)
            {
                // Add record to database
                record = await CreateResourceRecordAsync(input, logger);
            }
            else
            {
                // Update record in database
                record = await UpdateResourceRecordStatusAsync(input, logger);
            }

            // Trigger core continuation
            var result = await CreateResourceAsync(input, continuationToken, logger);

            // Update record resource id database
            record = await UpdateResourceRecordWithResultAsync(record, result, logger);

            // Last time through we want to finialize the resource
            if (string.IsNullOrEmpty(result.ContinuationToken))
            {
                logger.FluentAddValue("DidStatusUpdate", "true");

                // Finialize record in database
                record = await FinalizeResourceRecordAsync(input, logger);
            }

            return new ContinuationTaskMessageHandlerResult { Result = result };
        }

        protected abstract Task<ResourceCreateContinuationResult> CreateResourceAsync(
            CreateResourceContinuationInput input,
            string continuationToken,
            IDiagnosticsLogger logger);

        private async Task<ResourceRecord> CreateResourceRecordAsync(
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("ResourceCreatedRecord", true.ToString());

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
            };
            record.UpdateProvisioningStatus(OperationState.Initialized);

            // Update input
            input.Id = record.Id;
            input.ResourceGroup = $"RG-{id}";
            input.Subscription = SubscriptionCatalog.AzureSubscriptions.FirstOrDefault().SubscriptionId;

            // TODO: Update to get above info from the capacity manager

            // Create the actual record
            await ResourceRepository.CreateAsync(record, logger);

            return record;
        }

        private async Task<ResourceRecord> UpdateResourceRecordStatusAsync(
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            // If we where in Provisioning state, update it to be so
            var record = await ResourceRepository.GetAsync(input.Id, logger);

            // Only need to update if we have to do something
            if (record.UpdateProvisioningStatus(OperationState.InProgress))
            {
                logger.FluentAddValue("ResourceStatusUpdate", true.ToString());

                record = await ResourceRepository.UpdateAsync(record, logger);
            }

            return record;
        }

        private async Task<ResourceRecord> UpdateResourceRecordWithResultAsync(
            ResourceRecord record,
            ResourceCreateContinuationResult result,
            IDiagnosticsLogger logger)
        {
            // First time through we want to update the resource id
            if (record.AzureResourceInfo == null
                && result.AzureResourceInfo != null)
            {
                record.UpdateProvisioningStatus(OperationState.InProgress);
                record.AzureResourceInfo = result.AzureResourceInfo;

                record = await ResourceRepository.UpdateAsync(record, logger);
            }

            return record;
        }

        private async Task<ResourceRecord> FinalizeResourceRecordAsync(
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("ResourceFinalizeRecord", true.ToString());

            var record = await ResourceRepository.GetAsync(input.Id, logger);

            record.IsReady = true;
            record.Ready = DateTime.UtcNow;
            record.UpdateProvisioningStatus(OperationState.Succeeded);

            record = await ResourceRepository.UpdateAsync(record, logger);

            return record;
        }
    }
}
