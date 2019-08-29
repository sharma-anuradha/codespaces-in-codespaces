// <copyright file="DeleteResourceContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Delete compute continuation input.
    /// </summary>
    public class DeleteResourceContinuationInput
    {
        /// <summary>
        /// Gets or sets the target Resource Id.
        /// </summary>
        public Guid ResourceId { get; set; }
    }
}
