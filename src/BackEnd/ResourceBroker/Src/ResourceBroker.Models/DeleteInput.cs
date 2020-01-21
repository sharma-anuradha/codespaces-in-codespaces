// <copyright file="DeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Model required for Deallocation input.
    /// </summary>
    public class DeleteInput
    {
        /// <summary>
        /// Gets or sets a value indicating the target Resource Id.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the operation trigger.
        /// </summary>
        public string Trigger { get; set; }
    }
}
