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
        /// Post fix string for resource group.
        /// </summary>
        public const string ResourceGroupPostFix = "RG-CEResources";

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
        public string GetVirtualMachineAgentContainerName(string baseName)
        {
            return CreateResourceName(baseName);
        }

        /// <inheritdoc/>
        public string GetResourceGroupName(string baseName)
        {
            if (DeveloperPersonalStampSettings.DeveloperStamp)
            {
                return $"{GetUserName()}-{ResourceGroupPostFix}";
            }

            return baseName;
        }

        /// <inheritdoc/>
        public string GetArchiveStorageAccountName(string baseName)
        {
            if (DeveloperPersonalStampSettings.DeveloperStamp)
            {
                return $"{GetUserName()}{ResourceGroupPostFix}as".Replace("-", string.Empty).ToLowerInvariant();
            }

            return baseName;
        }

        private string CreateResourceName(string baseName)
        {
            if (DeveloperPersonalStampSettings.DeveloperStamp)
            {
                return $"{baseName}-{GetUserName()}";
            }

            return baseName;
        }

        private string GetUserName()
        {
            string userName;
            if (string.IsNullOrWhiteSpace(DeveloperPersonalStampSettings.DeveloperAlias))
            {
                userName = Environment.UserName;
            }
            else
            {
                userName = DeveloperPersonalStampSettings.DeveloperAlias;
            }

            if (string.IsNullOrWhiteSpace(userName) || userName.Equals("vsonline", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Username is not valid or is set to vsonline user.");
            }

            return userName.ToLowerInvariant();
        }
    }
}
