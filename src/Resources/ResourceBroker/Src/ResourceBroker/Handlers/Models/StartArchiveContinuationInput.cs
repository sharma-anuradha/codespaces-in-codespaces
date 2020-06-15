// <copyright file="StartArchiveContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Start compute continuation input.
    /// </summary>
    public class StartArchiveContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        public Guid EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the source storage resource id.
        /// </summary>
        public Guid FileShareResourceId { get; set; }
    }
}
