// <copyright file="ComponentInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Component input.
    /// </summary>
    public class ComponentInput
    {
        /// <summary>
        /// Gets or sets component id.
        /// </summary>
        public string ComponentId { get; set; }

        /// <summary>
        /// Gets or sets input.
        /// </summary>
        public CreateResourceContinuationInput Input { get; set; }

        /// <summary>
        /// Gets or sets status.
        /// </summary>
        public OperationState Status { get; set; }
    }
}