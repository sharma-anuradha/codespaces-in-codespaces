// <copyright file="StatusResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// The status result.
    /// </summary>
    public class StatusResult
    {
        /// <summary>
        /// Gets or sets the resource id token.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the resource location.
        /// </summary>
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the resource location.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status.
        /// </summary>
        public OperationState? ProvisioningStatus { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status Changed date.
        /// </summary>
        public DateTime? ProvisioningStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status.
        /// </summary>
        public OperationState? StartingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status Changed date.
        /// </summary>
        public DateTime? StartingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status.
        /// </summary>
        public OperationState? DeletingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status Changed date.
        /// </summary>
        public DateTime? DeletingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the cleanup status.
        /// </summary>
        public OperationState? CleanupStatus { get; set; }

        /// <summary>
        /// Gets or sets the cleanup Status Changed date.
        /// </summary>
        public DateTime? CleanupStatusChanged { get; set; }
    }
}
