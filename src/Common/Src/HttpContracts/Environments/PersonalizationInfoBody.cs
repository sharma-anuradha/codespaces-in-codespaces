// <copyright file="PersonalizationInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The environment personalization info.
    /// </summary>
    public class PersonalizationInfoBody
    {
        /// <summary>
        /// Gets or sets the dot-files repository.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesRepository")]
        public string DotfilesRepository { get; set; }

        /// <summary>
        /// Gets or sets the dot-files target path.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesTargetPath")]
        public string DotfilesTargetPath { get; set; }

        /// <summary>
        /// Gets or sets the dot-files install command.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "dotfilesInstallCommand")]
        public string DotfilesInstallCommand { get; set; }

        /// <summary>
        /// Gets or sets the default shell.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "defaultShell")]
        public string DefaultShell { get; set; }
    }
}
