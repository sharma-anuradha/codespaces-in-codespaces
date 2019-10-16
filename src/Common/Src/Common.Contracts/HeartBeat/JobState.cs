// <copyright file="JobState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Job outcome.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// Job started, and did not finish.
        /// </summary>
        Started = 0,

        /// <summary>
        /// Job succeeded.
        /// </summary>
        Succeeded = 1,

        /// <summary>
        /// Job failed.
        /// </summary>
        Failed = 2,
    }
}
