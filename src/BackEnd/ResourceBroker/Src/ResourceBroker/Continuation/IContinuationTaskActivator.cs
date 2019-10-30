// <copyright file="IContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// Continuation Activator which works with the supplied message and the
    /// available handlers to trigger the targetted handler.
    /// </summary>
    public interface IContinuationTaskActivator
    {
        /// <summary>
        /// Triggers the initial execution of the task.
        /// </summary>
        /// <param name="name">Target handler name for the job.</param>
        /// <param name="input">Input that should be passed to the handler.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="systemId">Custom tracking id if provided.</param>
        /// <param name="loggerProperties">Custom logging properties that should be flowed through the iterations.</param>
        /// <returns>Result of the execution.</returns>
        Task<ContinuationResult> Execute(string name, ContinuationInput input, IDiagnosticsLogger logger, Guid? systemId = null, IDictionary<string, string> loggerProperties = null);

        /// <summary>
        /// Carries on the next continuation.
        /// </summary>
        /// <param name="payload">Payload that needs to be continued.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Next payload.</returns>
        Task<ResourceJobQueuePayload> Continue(ResourceJobQueuePayload payload, IDiagnosticsLogger logger);
    }
}
