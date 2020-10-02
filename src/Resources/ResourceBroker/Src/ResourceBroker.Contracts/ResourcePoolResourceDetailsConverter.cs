// <copyright file="ResourcePoolResourceDetailsConverter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Json converter for ResourcePoolDetails type
    /// </summary>
    public class ResourcePoolResourceDetailsConverter : JsonTypeConverter
    {
        private static readonly Dictionary<string, Type> MapTypes
                = new Dictionary<string, Type>
            {
                    { "compute", typeof(ResourcePoolComputeDetails) },
                    { "keyVault", typeof(ResourcePoolKeyVaultDetails) },
                    { "osDisk", typeof(ResourcePoolOSDiskDetails) },
                    { "storage", typeof(ResourcePoolStorageDetails) },
            };

        protected override Type BaseType => typeof(ResourcePoolResourceDetails);

        protected override IDictionary<string, Type> SupportedTypes => MapTypes;
    }
}
