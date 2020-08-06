// <copyright file="CloudEnvironmentUpdate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Cloud environment settings to update an existing environment to.
    /// </summary>
    public class CloudEnvironmentUpdate
    {
        /// <summary>
        /// Gets or sets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the auto shutdown time the user specified.
        /// </summary>
        public int? AutoShutdownDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets the updated friendly name, or null if the name is not to be changed.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the plan to move to, or null if the plan is not to be changed.
        /// </summary>
        public VsoPlan Plan { get; set; }

        /// <summary>
        /// Gets or sets an identity that has access to the target plan, or null if
        /// the plan is not to be changed.
        /// </summary>
        public VsoClaimsIdentity PlanAccessIdentity { get; set; }
    }
}
