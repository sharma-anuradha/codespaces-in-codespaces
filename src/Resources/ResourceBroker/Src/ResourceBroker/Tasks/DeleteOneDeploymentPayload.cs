// <copyright file="DeleteOneDeploymentPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// A delete one deployment payload.
    /// </summary>
    public class DeleteOneDeploymentPayload : JobPayload
    {
        /// <summary>
        /// Gets or sets the subscription id.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the resource group name.
        /// </summary>
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the deployment id.
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        /// Gets or sets the deployment name.
        /// </summary>
        public string DeploymentName { get; set; }
    }
}
