// <copyright file="CreateNetworkInterfaceContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Create network resource continuation input.
    /// </summary>
    public class CreateNetworkInterfaceContinuationInput : CreateResourceContinuationInput
    {
        /// <summary>
        /// Gets or sets the subscription network resources should be created in.
        /// </summary>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the resource group network resources should be created in.
        /// </summary>
        public string ResourceGroup { get; set; }
    }
}
