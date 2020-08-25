// <copyright file="StatusResponseBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// The status request body.
    /// </summary>
    public class StatusResponseBody
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
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is ready.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Gets or sets the resource allocation created timestamp.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? ProvisioningStatus { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status Changed date.
        /// </summary>
        public DateTime? ProvisioningStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? StartingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status Changed date.
        /// </summary>
        public DateTime? StartingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? DeletingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status Changed date.
        /// </summary>
        public DateTime? DeletingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the cleanup status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? CleanupStatus { get; set; }

        /// <summary>
        /// Gets or sets the cleanup Status Changed date.
        /// </summary>
        public DateTime? CleanupStatusChanged { get; set; }
    }
}
