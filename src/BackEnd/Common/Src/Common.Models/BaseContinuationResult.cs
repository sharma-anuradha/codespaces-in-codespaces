// <copyright file="BaseContinuationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    ///
    /// </summary>
    public abstract class BaseContinuationResult
    {
        /// <summary>
        ///
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        ///
        /// </summary>
        public TimeSpan RetryAfter { get; set; }
    }
}
