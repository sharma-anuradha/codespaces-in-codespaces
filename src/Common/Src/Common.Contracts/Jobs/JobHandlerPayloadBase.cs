// <copyright file="JobHandlerPayloadBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// JobHandler base class that use just to payload to execute.
    /// </summary>
    /// <typeparam name="T">Type of the payload.</typeparam>
    public abstract class JobHandlerPayloadBase<T> : JobHandlerBase<T>
        where T : JobPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobHandlerPayloadBase{T}"/> class.
        /// </summary>
        /// <param name="dataflowBlockOptions">Optional TPL data flow block options.</param>
        /// <param name="options">Job handler options.</param>
        protected JobHandlerPayloadBase(ExecutionDataflowBlockOptions dataflowBlockOptions = null, JobHandlerOptions options = null)
            : base(dataflowBlockOptions, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHandlerPayloadBase{T}"/> class.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Parameter to control the TPL data flow execution options.</param>
        /// <param name="options">Job handler options.</param>
        protected JobHandlerPayloadBase(int maxDegreeOfParallelism, JobHandlerOptions options = null)
            : base(maxDegreeOfParallelism, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHandlerBase{T}"/> class.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Parameter to control the TPL data flow execution options.</param>
        /// <param name="boundedCapacity">Bound capacity.</param>
        /// <param name="options">Job handler options.</param>
        protected JobHandlerPayloadBase(int maxDegreeOfParallelism, int boundedCapacity, JobHandlerOptions options = null)
            : base(maxDegreeOfParallelism, boundedCapacity, options)
        {
        }

        /// <inheritdoc/>
        public override Task HandleJobAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return HandleJobAsync(job.Payload, logger, cancellationToken);
        }

        /// <summary>
        /// Process the payload.
        /// </summary>
        /// <param name="payload">The payload instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task HandleJobAsync(T payload, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
