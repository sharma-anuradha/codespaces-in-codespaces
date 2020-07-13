// <copyright file="IJobQueueConsumerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Definition of a job queue consumer factory.
    /// </summary>
    public interface IJobQueueConsumerFactory
    {
        /// <summary>
        /// Create a new instance of a job queue consumer.
        /// </summary>
        /// <param name="queueId">The job queue id.</param>
        /// <returns>Instance of anew job queue consumer.</returns>
        IJobQueueConsumer Create(string queueId);
    }

    /// <summary>
    /// A job queue consumer contract.
    /// </summary>
    public interface IJobQueueConsumer
    {
        /// <summary>
        /// Register a job handler to consume jobs.
        /// </summary>
        /// <typeparam name="T">Type of the payload.</typeparam>
        /// <param name="jobHandler">The job handler instance.</param>
        /// <param name="dataflowBlockOptions">TPL data flow options.</param>
        void RegisterJobHandler<T>(IJobHandler<T> jobHandler, ExecutionDataflowBlockOptions dataflowBlockOptions)
            where T : JobPayload;
    }
}
