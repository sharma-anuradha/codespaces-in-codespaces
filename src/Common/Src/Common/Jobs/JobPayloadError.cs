// <copyright file="JobPayloadError.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Payload job for exceptions that are found during processing the queue.
    /// </summary>
    public class JobPayloadError : JobPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobPayloadError"/> class.
        /// </summary>
        /// <param name="error">Job error to be reported.</param>
        public JobPayloadError(Exception error)
        {
            Error = error;
        }

        /// <summary>
        /// Gets the job queue processing error.
        /// </summary>
        public Exception Error { get; }
    }
}
