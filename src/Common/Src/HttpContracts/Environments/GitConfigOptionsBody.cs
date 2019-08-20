// <copyright file="GitConfigOptionsBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The environment Git configuraiton.
    /// </summary>
    public class GitConfigOptionsBody
    {
        /// <summary>
        /// Gets or sets the Git user name.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "userName")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the Git user email.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "userEmail")]
        public string UserEmail { get; set; }
    }
}
