// <copyright file="ResourceRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.Models
{
    /// <summary>
    /// Defines the a given resource pools current state.
    /// </summary>
    public class ResourcePoolSettingsRecord : TaggedEntity
    {
        /// <summary>
        /// Gets or sets a value indicating whether the pool should be enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the target count.
        /// </summary>
        public int? TargetCount { get; set; }
    }
}
