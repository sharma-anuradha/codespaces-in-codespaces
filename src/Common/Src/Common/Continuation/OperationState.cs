// <copyright file="OperationState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Tracks the state of operation in continuation framework.
    /// </summary>
    public enum OperationState
    {
        /// <summary>
        /// Operation completed with success.
        /// </summary>
        Succeeded = 1,

        /// <summary>
        /// Operation completed with failure.
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Operation got cancelled.
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Operation is in progress.
        /// </summary>
        InProgress = 4,

        /// <summary>
        /// Operation has not started yet.
        /// </summary>
        NotStarted = 5,

        /// <summary>
        /// Operation is not created yet.
        /// </summary>
        Initialized = 6,
    }
}
