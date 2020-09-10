// <copyright file="ExportCloudEnvironmentBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments
{
    /// <summary>
    /// Request body for the export environment request.
    /// </summary>
    public class ExportCloudEnvironmentBody
    {
        /// <summary>
        /// The type of export.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ExportType Type { get; set; }

        /// <summary>
        /// The name of the branch that should be created and the changes pushed to.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the secrets sent from Create/Resume request.
        /// </summary>
        public IEnumerable<SecretDataBody> Secrets { get; set; }
    }
}
