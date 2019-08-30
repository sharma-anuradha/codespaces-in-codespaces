// <copyright file="IContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public interface IContinuationTaskActivator
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="input"></param>
        /// <param name="logger"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> Execute(string name, object input, IDiagnosticsLogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<ContinuationTaskMessageHandlerResult> Continue(ResourceJobQueuePayload payload, IDiagnosticsLogger logger);
    }
}
