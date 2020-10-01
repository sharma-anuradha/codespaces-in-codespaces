// <copyright file="EnvironmentFinalizeExportActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Finalize Export Action Input.
    /// </summary>
    public class EnvironmentFinalizeExportActionInput : EnvironmentBaseFinalizeActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFinalizeExportActionInput"/> class.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        public EnvironmentFinalizeExportActionInput(Guid environmentId)
            : base(environmentId)
        {
        }

        /// <summary>
        /// Gets or sets exported environment blob url.
        /// </summary>
        public string EnvironmentExportBlobUrl { get; set; }

        /// <summary>
        /// The resulting branch where the changes were pushed
        /// </summary>
        public string ExportedBranch { get; set; }
    }
}
