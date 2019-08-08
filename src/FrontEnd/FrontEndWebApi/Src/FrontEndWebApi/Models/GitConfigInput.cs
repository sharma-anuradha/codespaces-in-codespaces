// <copyright file="GitConfigInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// The environment Git configuraiton.
    /// </summary>
    public class GitConfigInput
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
