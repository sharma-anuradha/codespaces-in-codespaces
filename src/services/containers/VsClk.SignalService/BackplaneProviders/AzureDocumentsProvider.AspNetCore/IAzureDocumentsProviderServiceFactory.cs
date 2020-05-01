// <copyright file="IAzureDocumentsProviderServiceFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Interface contract to create an Azure Document provider
    /// </summary>
    public interface IAzureDocumentsProviderServiceFactory
    {
        Task CreateAsync(
            (string ServiceId, string Stamp, string ServiceType) serviceInfo,
            DatabaseSettings databaseSettings,
            CancellationToken cancellationToken);
    }
}
