// <copyright file="ResumeCloudEnvironmentBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The REST API body for resuming a new Environment.
    /// </summary>
    public class ResumeCloudEnvironmentBody
    {
        /// <summary>
        /// Gets or sets the secrets from Create/Resume request.
        /// </summary>
        public IEnumerable<SecretDataBody> Secrets { get; set; }
    }
}