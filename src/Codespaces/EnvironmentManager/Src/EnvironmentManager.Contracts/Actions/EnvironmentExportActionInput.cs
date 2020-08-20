// <copyright file="EnvironmentExportActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment export action input.
    /// </summary>
    public class EnvironmentExportActionInput : EnvironmentBaseStartActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentExportActionInput"/> class.
        /// </summary>
        /// <param name="id">Target environment id.</param>
        public EnvironmentExportActionInput(Guid id)
            : base(id)
        {
        }

        /// <summary>
        /// Gets or sets start env params.
        /// </summary>
        public ExportCloudEnvironmentParameters ExportEnvironmentParams { get; set; }
    }
}