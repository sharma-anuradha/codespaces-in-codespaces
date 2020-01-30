// <copyright file="SuspendRequestBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// The suspend request body.
    /// </summary>
    public class SuspendRequestBody
    {
        /// <summary>
        /// Gets or sets the id of the target resource.
        /// </summary>
        public Guid ResourceId { get; set; }
    }
}
