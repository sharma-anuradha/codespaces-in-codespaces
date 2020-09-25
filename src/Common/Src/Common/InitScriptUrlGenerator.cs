// <copyright file="InitScriptUrlGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Init script url generator.
    /// </summary>
    public class InitScriptUrlGenerator : IInitScriptUrlGenerator
    {
        private const string LogBaseName = "init_script_url_generator";
        private const int ExpireInHours = 1;

        private const string ContainerName = "windows-init-shim";

        /// <summary>
        /// Name of the shim script which initializes a windows vm.
        /// The source of the shim script lives in the vsclk-cluster repository.
        /// When updated it release scripts for vsclk-cluster should be run on dev/ppe/prod to update the script on storage.
        /// Ideally no changes should be made to the shim script. Any init time additions should go to
        /// WindowsInit.ps1 in the Cascade repo.
        /// </summary>
        private const string BlobName = "WindowsInitShim.ps1";

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; set; }

        private IManagedCache ManagedCache { get; set; }

        /// <summary>
        /// Initializes a new instance of the InitScriptUrlGenerator class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane azure resource accessor.</param>
        public InitScriptUrlGenerator(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IManagedCache cache)
        {
            ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
            ManagedCache = Requires.NotNull(cache, nameof(cache));
        }

        /// <inheritdoc/>
        public async Task<string> GetInitScriptUrlAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_get_read_only_url",
                async (childLogger) =>
                {
                    var containerCacheKey = GetCacheKey(location, BlobName, ContainerName);
                    var container = await ManagedCache.GetAsync<CloudBlobContainer>(containerCacheKey, logger);

                    if (container == default)
                    {
                        var (accountName, accountKey) = await ControlPlaneAzureResourceAccessor
                                        .GetStampStorageAccountForComputeVmAgentImagesAsync(location);

                        var blobStorageClientOptions = new BlobStorageClientOptions
                        {
                            AccountName = accountName,
                            AccountKey = accountKey,
                        };

                        var blobClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));
                        container = blobClientProvider.GetCloudBlobContainer(ContainerName);

                        await ManagedCache.SetAsync(containerCacheKey, container, TimeSpan.FromHours(72), logger);
                    }

                    var blob = container.GetBlobReference(BlobName);

                    var policy = new SharedAccessBlobPolicy()
                    {
                        Permissions = SharedAccessBlobPermissions.Read,
                        SharedAccessExpiryTime = DateTime.UtcNow.Add(TimeSpan.FromHours(ExpireInHours)),
                    };

#pragma warning disable CA5377 // Use Container Level Access Policy
                    var sas = blob.GetSharedAccessSignature(policy);
#pragma warning restore CA5377 // Use Container Level Access Policy

                    return blob.Uri + sas;
                });
        }

        private string GetCacheKey(AzureLocation location, string resourceName, string containerName)
        {
            return $"{nameof(InitScriptUrlGenerator)}:{location.ToString().ToLowerInvariant()}:{resourceName}:{containerName}";
        }
    }
}
