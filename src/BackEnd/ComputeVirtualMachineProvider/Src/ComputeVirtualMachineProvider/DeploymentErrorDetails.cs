// <copyright file="DeploymentErrorDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Describes ARM Deployment errors.
    /// </summary>
    internal class DeploymentErrorDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentErrorDetails"/> class.
        /// </summary>
        public DeploymentErrorDetails()
        {
        }

        /// <summary>
        /// Gets or sets the resource id.
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public string StatusCode { get; internal set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; internal set; }
    }
}