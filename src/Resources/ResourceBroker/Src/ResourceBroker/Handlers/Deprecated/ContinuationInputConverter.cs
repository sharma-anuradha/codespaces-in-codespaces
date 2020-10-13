// <copyright file="ContinuationInputConverter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Json converter for ContinuationInput type
    /// </summary>
    public class ContinuationInputConverter : JsonTypeConverter
    {
        private static readonly Dictionary<string, Type> MapTypes
                = new Dictionary<string, Type>
            {
                    { "computeVM", typeof(VirtualMachineProviderCreateInput) },
                    { "withComponent", typeof(CreateResourceWithComponentInput) },
                    { "fileShare", typeof(FileShareProviderCreateInput) },
                    { "queue", typeof(QueueProviderCreateInput) },
                    { "keyVault", typeof(KeyVaultProviderCreateInput) },
                    { "network", typeof(NetworkInterfaceProviderCreateInput) },
            };

        protected override Type BaseType => typeof(ContinuationInput);

        protected override IDictionary<string, Type> SupportedTypes => MapTypes;
    }
}
