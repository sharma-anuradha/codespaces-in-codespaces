// <copyright file="EnvironmentRepairActions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows
{
    /// <summary>
    /// Actions to repair environments.
    /// </summary>
    public enum EnvironmentRepairActions
    {
        /// <summary>
        /// Do nothing.
        /// </summary>
        None = 0,

        /// <summary>
        /// Suspend Environment.
        /// </summary>
        ForceSuspend = 1,

        /// <summary>
        /// Mark an environment as Unavailable.
        /// </summary>
        Unavailable = 2,

        /// <summary>
        /// Mark an environment as Failed.
        /// </summary>
        Fail = 3,
    }
}
