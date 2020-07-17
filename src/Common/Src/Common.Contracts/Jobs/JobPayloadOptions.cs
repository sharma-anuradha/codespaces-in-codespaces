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
        ///  gets or sets the initial visbility timeout.
        /// </summary>
        public TimeSpan? InitialVisibilityDelay { get; set; }
    }
}
