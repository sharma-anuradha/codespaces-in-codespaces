// <copyright file="BaseContinuationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    ///
    /// </summary>
    public class ContinuationResult
    {
        /// <summary>
        /// Gets / Sets the operation state.
        /// </summary>
        public OperationState Status { get; set; }

        /// <summary>
        /// Gets / sets the continuation token for operation.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Gets / Sets the time to retry after current call.
        /// </summary>
        public TimeSpan RetryAfter { get; set; }

        /// <summary>
        /// Gets / Sets the input for next operation phase.
        /// </summary>
        public object NextInput { get; set; }
    }
}
