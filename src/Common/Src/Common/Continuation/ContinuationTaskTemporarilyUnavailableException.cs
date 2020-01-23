// <copyright file="ContinuationTaskTemporarilyUnavailableException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Operation Temporarily Unavailable exception.
    /// </summary>
    public class ContinuationTaskTemporarilyUnavailableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskTemporarilyUnavailableException"/> class.
        /// </summary>
        /// <param name="message">Target exception message.</param>
        /// <param name="retryAfter">When the operation should be retried.</param>
        /// <param name="innerException">Target inneer exception.</param>
        public ContinuationTaskTemporarilyUnavailableException(
            string message, TimeSpan retryAfter, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets the time to retry after current call.
        /// </summary>
        public TimeSpan RetryAfter { get; }
    }
}
