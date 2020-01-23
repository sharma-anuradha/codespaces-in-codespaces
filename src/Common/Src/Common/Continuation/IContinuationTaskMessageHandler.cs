// <copyright file="IContinuationTaskMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Handles queue messages.
    /// </summary>
    public interface IContinuationTaskMessageHandler
    {
        /// <summary>
        /// Determines if it can process queue message.
        /// </summary>
        /// <param name="payload">Queue message.</param>
        /// <returns>result.</returns>
        bool CanHandle(ContinuationQueuePayload payload);

        /// <summary>
        /// Process Queue Message.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger);
    }
}
