// <copyright file="FeatureToggleCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using CommandLine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Toggle a feature on or off.
    /// </summary>
    [Verb("toggle-feature", HelpText = "Toggle a feature on or off.")]
    public class FeatureToggleCommand : SystemConfigurationCommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating the feature id.
        /// </summary>
        [Option("feature", HelpText = "The feature.", Required = true)]
        public string Feature { get; set; }

        /// <summary>
        /// Gets or sets a value indicating for the feature.
        /// </summary>
        [Option("value", HelpText = "True if the feature should be enabled or False otherwise.")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the feature-flag entry should be removed from the database.
        /// </summary>
        [Option("remove", HelpText = "Remove the feature flag entry from the configuration database.")]
        public bool Remove { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            string value;

            if (Remove)
            {
                if (Value != null)
                {
                    throw new Exception("Invalid use of both --remove and --value.");
                }

                value = null;
            }
            else if (!string.IsNullOrEmpty(Value))
            {
                if (!bool.TryParse(Value, out var enabled))
                {
                    throw new Exception($"Invalid value specified for --value: {Value}");
                }

                value = enabled ? "true" : "false";
            }
            else
            {
                throw new Exception($"No value specified.");
            }

            UpdateSystemConfigurationAsync(services, $"featureflag:{Feature}", value, stdout, stderr).Wait();
        }
    }
}
