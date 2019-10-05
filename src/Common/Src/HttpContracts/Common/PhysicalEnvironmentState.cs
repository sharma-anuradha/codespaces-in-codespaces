// <copyright file="PhysicalEnvironmentState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Represents the current state of a Cloud Environment instance running on a Virtual Machine.
    /// </summary>
    [Flags]
    public enum EnvironmentRunningState
    {
        /// <summary>
        /// Unknown state.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Docker daemon is running (Linux only).
        /// </summary>
        DockerDaemonRunning = 1,

        /// <summary>
        /// Cloud environments container is running (Linux only).
        /// </summary>
        ContainerRunning = 2,

        /// <summary>
        /// CLI Bootstrap process is running.
        /// </summary>
        CliBootstrapRunning = 4,

        /// <summary>
        /// Liveshare Agent is running.
        /// </summary>
        VslsAgentRunning = 8,
    }
}
