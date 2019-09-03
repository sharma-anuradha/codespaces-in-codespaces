// <copyright file="IContinuationTaskMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    ///
    /// </summary>
    public interface IContinuationTaskMessageHandler
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        bool CanHandle(ResourceJobQueuePayload payload);

        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger);
    }
}
