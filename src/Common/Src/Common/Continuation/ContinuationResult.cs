// <copyright file="ContinuationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Represents the continuation result.
    /// </summary>
    public class ContinuationResult
    {
        /// <summary>
        /// Gets or sets the operation state.
        /// </summary>
        public OperationState Status { get; set; }

        /// <summary>
        /// Gets or sets the time to retry after current call.
        /// </summary>
        public TimeSpan RetryAfter { get; set; }

        /// <summary>
        /// Gets or sets the input for next operation phase.
        /// </summary>
        public ContinuationInput NextInput { get; set; }

        /// <summary>
        /// Gets or sets the error reason in case of failure.
        /// </summary>
        public string ErrorReason { get; set; }
    }
}
