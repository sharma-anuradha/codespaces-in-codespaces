// <copyright file="UpdateCloudEnvironmentBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The REST API body for updating an existing Environment.
    /// </summary>
    public class UpdateCloudEnvironmentBody
    {
        /// <summary>
        /// Gets or sets the updated SKU name, or null if the SKU is not to be changed.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the updated auto shutdown time, or null if the value is not to be changed.
        /// </summary>
        public int? AutoShutdownDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets the updated friendly name, or null if the name is not to be changed.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the updated plan ID (fully-qualified Azure resource id), or null if the
        /// plan is not to be changed.
        /// </summary>
        /// <remarks>
        /// Changing the plan is a request to _move_ the environment out of the current plan
        /// and into another plan. Some limitations (only moving within the same location)
        /// or quotas (maximum environments in the other plan) may block a move between plans.
        /// </remarks>
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets a token that grants access to the target plan, required when
        /// moving an environment into a different plan.
        /// </summary>
        public string PlanAccessToken { get; set; }
    }
}