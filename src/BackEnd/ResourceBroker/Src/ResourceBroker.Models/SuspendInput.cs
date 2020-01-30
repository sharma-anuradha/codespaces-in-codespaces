// <copyright file="SuspendInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Model required for Suspend input.
    /// </summary>
    public class SuspendInput
    {
        /// <summary>
        /// Gets or sets a value indicating the target Resource Id.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the operation trigger.
        /// </summary>
        public string Trigger { get; set; }

        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        public Guid EnvironmentId { get; set; }
    }
}
