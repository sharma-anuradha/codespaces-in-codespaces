// <copyright file="SuspendInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
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
    }
}
