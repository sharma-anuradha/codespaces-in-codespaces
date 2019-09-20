// <copyright file="ResourceNameBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Resource builder class.
    /// </summary>
    public class ResourceNameBuilder : IResourceNameBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNameBuilder"/> class.
        /// </summary>
        /// <param name="personalStampSettings">Developer personal stamp settings.</param>
        public ResourceNameBuilder(DeveloperPersonalStampSettings personalStampSettings)
        {
            DeveloperPersonalStampSettings = personalStampSettings;
        }

        private DeveloperPersonalStampSettings DeveloperPersonalStampSettings { get; }

        /// <inheritdoc/>
        public string GetCosmosDocDBName(string baseName)
        {
            return CreateResourceName(baseName);
        }

        /// <inheritdoc/>
        public string GetLeaseName(string baseName)
        {
            return CreateResourceName(baseName);
        }

        /// <inheritdoc/>
        public string GetQueueName(string baseName)
        {
            return CreateResourceName(baseName);
        }

        /// <inheritdoc/>
        public string GetResourceGroupName(string baseName)
        {
            if (DeveloperPersonalStampSettings.DeveloperStamp)
            {
                return $"{Environment.UserName}-RG-CEResources";
            }

            return baseName;
        }

        private string CreateResourceName(string baseName)
        {
            if (DeveloperPersonalStampSettings.DeveloperStamp)
            {
                return $"{baseName}-{Environment.UserName}";
            }

            return baseName;
        }
    }
}
