// <copyright file="PersonalizationInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The environment personalization info.
    /// </summary>
    public class PersonalizationInfo
    {
        /// <summary>
        /// Gets or sets the dot-files repository.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesRepository")]
        public string DotfilesRepository { get; set; }

        /// <summary>
        /// Gets or sets the dot-files target path.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesTargetPath")]
        public string DotfilesTargetPath { get; set; }

        /// <summary>
        /// Gets or sets the dot-files install command.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesInstallCommand")]
        public string DotfilesInstallCommand { get; set; }
    }
}
