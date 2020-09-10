// <copyright file="StartCloudEnvironmentParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Parameters required to start the compute of a CloudEnvironment.
    /// </summary>
    public class StartCloudEnvironmentParameters : CloudEnvironmentParameters
    {
        /// <summary>
        /// Gets or sets the connection service (Live Share) uri.
        /// </summary>
        public Uri ConnectionServiceUri { get; set; }
    }
}
