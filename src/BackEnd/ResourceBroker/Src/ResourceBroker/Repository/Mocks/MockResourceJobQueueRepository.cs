// <copyright file="MockResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks
{
    /// <summary>
    /// Mock resource job queue repository.
    /// </summary>
    public class MockResourceJobQueueRepository : IResourceJobQueueRepository
    {
        /// <summary>
        /// Queue name that should be used.
        /// </summary>
        public const string QueueName = "mock-resource-job-queue";

        /// <summary>
        /// Initializes a new instance of the <see cref="MockResourceJobQueueRepository"/> class.
        /// </summary>
        /// <param name="backgroundJobs">Background job queue to use.</param>
        /// <param name="resourceRepository">Resource repository.</param>
        /// <param name="logger">Target logger.</param>
        public MockResourceJobQueueRepository(
            IBackgroundJobClient backgroundJobs,
            IResourceRepository resourceRepository,
            IDiagnosticsLogger logger)
        {
            BackgroundJobs = backgroundJobs;
            ResourceRepository = resourceRepository;
            Logger = logger;
            EnqueuedState = new EnqueuedState
                {
                    Queue = QueueName,
                };
            Random = new Random();
        }

        private IBackgroundJobClient BackgroundJobs { get; }

        private IResourceRepository ResourceRepository { get; }

        private IDiagnosticsLogger Logger { get; }

        private IState EnqueuedState { get; }

        private Random Random { get; }

        /// <inheritdoc/>
        public Task AddAsync(string id, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            // TODO: Delay add to queue if needed
            BackgroundJobs.Create(() => AddToQueue(id), EnqueuedState);

            return Task.CompletedTask;
        }

        /// <summary>
        /// This is public due to the need to HangFire to use public methods.
        /// </summary>
        /// <param name="id">Id of the item in the queue.</param>
        /// <returns>Task that is being placed on the queue which will, when trigged,
        /// will start the processing the resource provisioning.</returns>
        public async Task AddToQueue(string id)
        {
            // TODO: Need to update once I get to job processing, thisis just a
            //       sketch of what should happen.

            // Simulate updating cosmos db so it knows that the item has be picked up
            // and is currently being processed.
            var resource = await ResourceRepository.GetAsync(id, Logger);

            resource.UpdateProvisioningStatus(OperationState.InProgress);

            await ResourceRepository.UpdateAsync(resource, Logger);

            // Simulate delay of the resource in question being created
            await Task.Delay(Random.Next(2000, 5000));

            // Simulate the updating of the record status post the create happening
            resource = await ResourceRepository.GetAsync(id, Logger);

            resource.IsReady = true;
            resource.Ready = DateTime.UtcNow;
            resource.UpdateProvisioningStatus(OperationState.Succeeded);

            await ResourceRepository.UpdateAsync(resource, Logger);
        }

        public Task<IEnumerable<CloudQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<CloudQueueMessage> GetAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CloudQueueMessage message, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
