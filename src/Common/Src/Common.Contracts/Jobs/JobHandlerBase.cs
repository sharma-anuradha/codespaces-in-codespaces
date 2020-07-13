// <copyright file="JobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// JobHandler base class reference implementation.
    /// </summary>
    /// <typeparam name="T">Type of the payload.</typeparam>
    public abstract class JobHandlerBase<T> : IJobHandler<T>, IJobHandlerOptions
        where T : JobPayload
    {
        private static readonly TimeSpan DefaultJobWaiting = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan DefaultJobUpdate = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHandlerBase{T}"/> class.
        /// </summary>
        /// <param name="dataflowBlockOptions">Optional TPL data flow block options.</param>
        protected JobHandlerBase(ExecutionDataflowBlockOptions dataflowBlockOptions = null)
        {
            DataflowBlockOptions = dataflowBlockOptions ?? new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            };
        }

        /// <inheritdoc/>
        public virtual ExecutionDataflowBlockOptions DataflowBlockOptions { get; }

        /// <inheritdoc/>
        public async Task HandleJobAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var updateTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(DefaultJobWaiting, cts.Token);
                        await job.UpdateAsync(DefaultJobUpdate, cts.Token);
                    }
                });
                try
                {
                    await HandleJobInternalAsync(job, logger, cancellationToken);
                    await job.DisposeAsync();
                }
                finally
                {
                    cts.Cancel();
                }
            }
        }

        /// <summary>
        /// Process the payload.
        /// </summary>
        /// <param name="job">The job instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task HandleJobInternalAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
