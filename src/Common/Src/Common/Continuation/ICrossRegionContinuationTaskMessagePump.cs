// <copyright file="ICrossRegionContinuationTaskMessagePump.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Message pump which gates messages to/from the underlying queue.
    /// </summary>
    public interface ICrossRegionContinuationTaskMessagePump : IDisposable
    {
        /// <summary>
        /// Pushes message onto queue.
        /// </summary>
        /// <param name="payload">Payload to be pushed.</param>
        /// <param name="controlPlaneRegion">Control plane region.</param>
        /// <param name="initialVisibilityDelay">Initial visibility delay.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task PushMessageToControlPlaneRegionAsync(ContinuationQueuePayload payload, AzureLocation controlPlaneRegion, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger);
    }
}
