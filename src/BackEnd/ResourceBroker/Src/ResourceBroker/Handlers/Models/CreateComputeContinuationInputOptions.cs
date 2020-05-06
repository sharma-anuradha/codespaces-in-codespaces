// <copyright file="CreateComputeContinuationInputOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Create compute resource continuation input options.
    /// </summary>
    public class CreateComputeContinuationInputOptions : CreateResourceContinuationInputOptions
    {
        /// <summary>
        /// Gets or sets the backing OS disk resource id.
        /// </summary>
        public string OSDiskResourceId { get; set; }
    }
}
