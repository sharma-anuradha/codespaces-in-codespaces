// <copyright file="UpdateConfigSettingCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using CommandLine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Update system configuration settings.
    /// </summary>
    [Verb("update-settings", HelpText = "Update system configuration settings.")]
    public class UpdateConfigSettingCommand : SystemConfigurationCommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating the setting id.
        /// </summary>
        [Option("setting", HelpText = "The system configuration setting.", Required = true)]
        public string Setting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the value of the setting.
        /// </summary>
        [Option("value", HelpText = "The system configuration setting value.", Required = true)]
        public string Value { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            UpdateSystemConfigurationAsync(services, $"setting:{Setting}", Value, stdout, stderr).Wait();
        }
    }
}
