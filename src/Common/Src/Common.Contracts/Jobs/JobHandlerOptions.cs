// <copyright file="JobHandlerOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// The job handler options.
    /// </summary>
    public class JobHandlerOptions
    {
        /// <summary>
        /// Gets or sets the handler timeout value.
        /// </summary>
        public TimeSpan? HandlerTimout { get; set; }

        /// <summary>
        /// Gets or sets the max retries when processed by a job handler.
        /// </summary>
        public int? MaxHandlerRetries { get; set; }

        /// <summary>
        /// Create job handler options with max retries.
        /// </summary>
        /// <param name="maxHandlerRetries">Number of retries.</param>
        /// <returns>Job handler options instance.</returns>
        public static JobHandlerOptions WithRetries(int maxHandlerRetries)
        {
            return new JobHandlerOptions() { MaxHandlerRetries = maxHandlerRetries };
        }
    }
}
