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
        /// Mark an environment as Unavailable.
        /// </summary>
        Unavailable = 2,

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
