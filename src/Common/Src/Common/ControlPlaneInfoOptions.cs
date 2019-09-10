// <copyright file="ControlPlaneInfoOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Service principal constructor options.
    /// </summary>
    public class ControlPlaneInfoOptions
    {
        /// <summary>
        /// Gets or sets the azure resource settings settings.
        /// </summary>
        public ControlPlaneSettings ControlPlaneSettings { get; set; }
    }
}
