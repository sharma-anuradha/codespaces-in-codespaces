// <copyright file="ConfigurationHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <summary>
    /// Configuration helpers.
    /// </summary>
    public static class ConfigurationHelpers
    {
        public static string GetCompleteKey(string scope, ConfigurationType configurationType, string componentName, string configurationName)
        {
            string configType = configurationType.ToString();
            string keyName = $"{componentName}-{configurationName}";

            // return the lower case version
            return $"{configType}:{scope}:{keyName}".ToLower();
        }
    }
}
