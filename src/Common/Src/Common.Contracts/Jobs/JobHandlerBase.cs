// <copyright file="JobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// The non generic job payload base.
    /// </summary>
    public class JobHandlerBase
    {
        /// <summary>
        /// Default dataflow block options.
        /// </summary>
        public static readonly ExecutionDataflowBlockOptions DefaultDataflowBlockOptions = new ExecutionDataflowBlockOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
        };

        /// <summary>
        /// Option for single processor.
        /// </summary>
        public static readonly ExecutionDataflowBlockOptions NoParallelismDataflowBlockOptions = new ExecutionDataflowBlockOptions()
        {
            MaxDegreeOfParallelism = 1,
        };

        /// <summary>
        /// Create a dataflow execute options.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Max degree of parallelism.</param>
        /// <param name="boundedCapacity">Bound capacity.</param>
        /// <param name="ensureOrdered">If ensure ordered.</param>
        /// <returns>An execute dataflow block options.</returns>
        public static ExecutionDataflowBlockOptions WithValues(
            int? maxDegreeOfParallelism = null,
            int? boundedCapacity = null,
            bool? ensureOrdered = null)
        {
            var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions();
            if (maxDegreeOfParallelism.HasValue)
            {
                executionDataflowBlockOptions.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value;
            }

            if (boundedCapacity.HasValue)
            {
                executionDataflowBlockOptions.BoundedCapacity = boundedCapacity.Value;
            }

            if (ensureOrdered.HasValue)
            {
                executionDataflowBlockOptions.EnsureOrdered = ensureOrdered.Value;
            }

            return executionDataflowBlockOptions;
        }
    }

    /// <summary>
    /// JobHandler base class reference implementation.
    /// </summary>
    /// <typeparam name="T">Type of the payload.</typeparam>
#pragma warning disable SA1402 // File may only contain a single type
    public abstract class JobHandlerBase<T> : JobHandlerBase, IJobHandler<T>, IJobHandlerOptions
#pragma warning restore SA1402 // File may only contain a single type
        where T : JobPayload
    {
        private static readonly TimeSpan DefaultJobWaiting = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan DefaultJobUpdate = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHandlerBase{T}"/> class.
        /// </summary>
        /// <param name="dataflowBlockOptions">Optional TPL data flow block options.</param>
        /// <param name="options">Job handler options.</param>
        protected JobHandlerBase(ExecutionDataflowBlockOptions dataflowBlockOptions = null, JobHandlerOptions options = null)
        {
            DataflowBlockOptions = dataflowBlockOptions ?? DefaultDataflowBlockOptions;
            Options = options;
        }

        /// <inheritdoc/>
        public virtual ExecutionDataflowBlockOptions DataflowBlockOptions { get; }

        /// <inheritdoc/>
        public JobHandlerOptions Options { get; }

        /// <inheritdoc/>
        public async Task HandleJobAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                int updateCount = 0;
                var updateTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(DefaultJobWaiting, cts.Token);
                        await job.UpdateAsync(DefaultJobUpdate, cts.Token);
                        ++updateCount;
                    }
                });
                try
                {
                    await HandleJobInternalAsync(job, logger, cancellationToken);
                    await job.DisposeAsync();
                }
                finally
                {
                    logger.FluentAddValue("UpdateCount", updateCount);
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
