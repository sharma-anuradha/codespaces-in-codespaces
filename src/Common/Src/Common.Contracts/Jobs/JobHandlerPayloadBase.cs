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
        protected JobHandlerPayloadBase(ExecutionDataflowBlockOptions dataflowBlockOptions = null)
            : base(dataflowBlockOptions)
        {
        }

        /// <inheritdoc/>
        protected override Task HandleJobInternalAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
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
