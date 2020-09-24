// <copyright file="JobHandlerOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

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
        public TimeSpan? HandlerTimeout { get; set; }

        /// <summary>
        /// Gets or sets the max retries when processed by a job handler.
        /// </summary>
        public int? MaxHandlerRetries { get; set; }

        /// <summary>
        /// Gets or sets the retry timeout value.
        /// </summary>
        public TimeSpan? RetryTimeout { get; set; }

        /// <summary>
        /// Gets or sets the invisible threshold time span.
        /// </summary>
        public TimeSpan? InvisibleThreshold { get; set; }

        /// <summary>
        /// Gets or sets the expiration timeout.
        /// </summary>
        public TimeSpan? ExpireTimeout { get; set; }

        /// <summary>
        /// Gets or sets the optional set of error callbacks.
        /// </summary>
        public IEnumerable<IJobHandlerErrorCallback> ErrorCallbacks { get; set; }

        /// <summary>
        /// Create job handler options with max retries.
        /// </summary>
        /// <param name="maxHandlerRetries">Number of retries.</param>
        /// <returns>Job handler options instance.</returns>
        public static JobHandlerOptions WithValues(int maxHandlerRetries)
        {
            return new JobHandlerOptions() { MaxHandlerRetries = maxHandlerRetries };
        }
    }
}
