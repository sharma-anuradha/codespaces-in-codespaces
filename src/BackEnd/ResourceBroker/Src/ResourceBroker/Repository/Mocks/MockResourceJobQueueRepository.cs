// <copyright file="MockResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks
{
    /// <summary>
    ///
    /// </summary>
    public class MockResourceJobQueueRepository : IResourceJobQueueRepository
    {
        public const string QueueName = "mock-resource-job-queue";

        /// <summary>
        ///
        /// </summary>
        /// <param name="backgroundJobs"></param>
        /// <param name="jobStorage"></param>
        /// <param name="resourceRepository"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="logValues"></param>
        public MockResourceJobQueueRepository(
            IBackgroundJobClient backgroundJobs,
            JobStorage jobStorage,
            IResourceRepository resourceRepository,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet logValues)
        {
            BackgroundJobs = backgroundJobs;
            JobStorage = jobStorage;
            ResourceRepository = resourceRepository;
            Logger = loggerFactory.New(logValues);
            EnqueuedState = new EnqueuedState
                {
                    Queue = QueueName,
                };
            Random = new Random();
        }

        private IBackgroundJobClient BackgroundJobs { get; }

        private JobStorage JobStorage { get; }

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

        /// <inheritdoc/>
        public async Task<int> GetQueuedCountAsync(string skuName, ResourceType type, string location, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(250, 2500));

            var monitor = JobStorage.GetMonitoringApi();

            var count = (int)monitor.EnqueuedCount(EnqueuedState.Name);

            return count;
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

        public Task<IEnumerable<IResourceJobQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<IResourceJobQueueMessage> GetAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(IResourceJobQueueMessage message, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
