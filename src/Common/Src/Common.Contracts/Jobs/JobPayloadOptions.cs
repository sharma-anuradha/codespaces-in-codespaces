// <copyright file="JobPayloadOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// The job payload options.
    /// </summary>
    public class JobPayloadOptions
    {
        /// <summary>
        ///  gets or sets the initial visbility timeout.
        /// </summary>
        public TimeSpan? InitialVisibilityDelay { get; set; }

        /// <summary>
        /// Gets or sets the handler timeout value.
        /// </summary>
        public TimeSpan? HandlerTimout { get; set; }

        /// <summary>
        /// Gets or sets the max retries when processed by a job handler.
        /// </summary>
        public int? MaxHandlerRetries { get; set; }
    }
}
