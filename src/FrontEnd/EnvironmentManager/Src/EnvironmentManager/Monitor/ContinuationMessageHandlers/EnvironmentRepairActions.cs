// <copyright file="EnvironmentRepairActions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Actions to repair environments.
    /// </summary>
    public enum EnvironmentRepairActions
    {
        /// <summary>
        /// Suspend Environment.
        /// </summary>
        ForceSuspend = 1,

        /// <summary>
        /// Do nothing.
        /// </summary>
        None = 0,
    }
}
