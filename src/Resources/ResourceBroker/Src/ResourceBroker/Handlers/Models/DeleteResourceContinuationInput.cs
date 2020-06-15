// <copyright file="DeleteResourceContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Delete compute continuation input.
    /// </summary>
    public class DeleteResourceContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets the Environment Id.
        /// </summary>
        public Guid? EnvironmentId { get; set; }
    }
}
