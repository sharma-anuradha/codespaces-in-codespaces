// <copyright file="MockResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

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
        public const string LogBaseName = "mock_resource_job_queue";

        /// <summary>
        /// Initializes a new instance of the <see cref="MockResourceJobQueueRepository"/> class.
        /// </summary>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="resourceRepository">Resource repository.</param>
        public MockResourceJobQueueRepository(
            ITaskHelper taskHelper,
            IResourceRepository resourceRepository)
        {
            TaskHelper = taskHelper;
            ResourceRepository = resourceRepository;
            Random = new Random();
        }

        private ITaskHelper TaskHelper { get; }

        private IResourceRepository ResourceRepository { get; }

        private IDiagnosticsLogger Logger { get; }

        private Random Random { get; }

        /// <inheritdoc/>
        public Task AddAsync(string id, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            // TODO: Delay add to queue if needed
            TaskHelper.RunBackground(
                LogBaseName, (childLogger) => AddToQueue(id), delay: TimeSpan.FromMilliseconds(250));

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

            resource.UpdateProvisioningStatus(OperationState.InProgress, "PreAddToQueue");

            await ResourceRepository.UpdateAsync(resource, Logger);

            // Simulate delay of the resource in question being created
            await Task.Delay(Random.Next(2000, 5000));

            // Simulate the updating of the record status post the create happening
            resource = await ResourceRepository.GetAsync(id, Logger);

            resource.IsReady = true;
            resource.Ready = DateTime.UtcNow;
            resource.UpdateProvisioningStatus(OperationState.Succeeded, "PostAddToQueue");

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
