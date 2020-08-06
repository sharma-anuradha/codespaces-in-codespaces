// <copyright file="JobPayloadOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// The job payload options.
    /// </summary>
    public class JobPayloadOptions : JobHandlerOptions
    {
        /// <summary>
        ///  Gets or sets the initial visbility timeout.
        /// </summary>
        public TimeSpan? InitialVisibilityDelay { get; set; }

        /// <summary>
        /// Gets or sets the expiration timeout.
        /// </summary>
        public TimeSpan? ExpireTimeout { get; set; }

        /// <summary>
        /// Gets or sets the invisible threshold time span.
        /// </summary>
        public TimeSpan? InvisibleThreshold { get; set; }
    }
}
