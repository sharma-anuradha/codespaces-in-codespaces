// <copyright file="IContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public interface IContinuationTaskActivator
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationTaskMessageHandlerResult> Execute(IContinuationTaskMessageHandler handler, object input, string name, IDiagnosticsLogger logger);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<ContinuationTaskMessageHandlerResult> Execute(IContinuationTaskMessageHandler handler, ResourceJobQueuePayload payload, IDiagnosticsLogger logger);
    }
}
