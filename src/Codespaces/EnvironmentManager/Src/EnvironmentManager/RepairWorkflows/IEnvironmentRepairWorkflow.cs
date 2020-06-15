// <copyright file="IEnvironmentRepairWorkflow.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows
{
    /// <summary>
    /// Represents Workflows for Environment Repair.
    /// </summary>
    public interface IEnvironmentRepairWorkflow
    {
        /// <summary>
        /// Gets Type of workflow.
        /// </summary>
        EnvironmentRepairActions WorkflowType { get; }

        /// <summary>
        /// Execute Workflow.
        /// </summary>
        /// <param name="cloudEnvironment">Target cloud environment.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>task.</returns>
        Task ExecuteAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger);
    }
}
