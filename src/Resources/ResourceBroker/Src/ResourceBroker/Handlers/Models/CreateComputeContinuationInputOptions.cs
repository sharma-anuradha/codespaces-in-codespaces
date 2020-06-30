// <copyright file="CreateComputeContinuationInputOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

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

        /// <summary>
        /// Gets or sets the Azure subnet info.
        /// </summary>
        public string SubnetResourceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the compute go through a hardboot, slower but reliable.
        /// </summary>
        public bool HardBoot { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the agent should be updated.
        /// </summary>
        public bool UpdateAgent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the OS disk record should be created if it doesn't exist already.
        /// </summary>
        public bool CreateOSDiskRecord { get; set; }
    }
}
