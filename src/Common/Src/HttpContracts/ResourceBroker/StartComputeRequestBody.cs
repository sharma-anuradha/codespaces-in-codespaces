// <copyright file="StartComputeRequestBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// A request to start compute with storage and env vars.
    /// </summary>
    public class StartComputeRequestBody
    {
        /// <summary>
        /// Gets or sets the storage resource id token.
        /// </summary>
        public string StorageResourceIdToken { get; set; }

        /// <summary>
        /// Gets or sets the environment variable dictionary for the environment compute.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; }
    }
}
