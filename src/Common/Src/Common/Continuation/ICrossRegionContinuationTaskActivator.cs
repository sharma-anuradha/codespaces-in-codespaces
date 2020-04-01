// <copyright file="ICrossRegionContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Cross Region Continuation Activator which works with the supplied message and the
    /// available handlers to trigger the targetted handler.
    /// </summary>
    public interface ICrossRegionContinuationTaskActivator
    {
        /// <summary>
        /// Triggers the task execution in the control plane region corresponding to the given data plane region.
        /// </summary>
        /// <param name="name">Target handler name for the job.</param>
        /// <param name="dataPlaneRegion">Data plane region.</param>
        /// <param name="input">Input that should be passed to the handler.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="systemId">Custom tracking id if provided.</param>
        /// <param name="loggerProperties">Custom logging properties that should be flowed through the iterations.</param>
        /// <returns>Result of the execution.</returns>
        Task<ContinuationResult> ExecuteForDataPlane(
            string name,
            AzureLocation dataPlaneRegion,
            ContinuationInput input,
            IDiagnosticsLogger logger,
            Guid? systemId = null,
            IDictionary<string, string> loggerProperties = null);
    }
}
