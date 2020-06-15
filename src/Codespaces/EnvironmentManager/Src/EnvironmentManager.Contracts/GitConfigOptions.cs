// <copyright file="GitConfigOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The Git configuration options.
    /// </summary>
    public class GitConfigOptions
    {
        /// <summary>
        /// Gets or sets the Git user name.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "userName")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the Git user email.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "userEmail")]
        public string UserEmail { get; set; }
    }
}
