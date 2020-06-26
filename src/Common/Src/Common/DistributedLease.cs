// <copyright file="DistributedLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Allows components to obtain a distributed lease lock. This allows for distributed
    /// components to know whether another component is working on a given resource and
    /// either wait for it to be freed, or move on.
    /// </summary>
    public class DistributedLease : IDistributedLease
    {
        private static TimeSpan leaseTime = TimeSpan.FromMinutes(1);
        private static TimeSpan autoRenewLeaseTime = leaseTime - TimeSpan.FromSeconds(7.5);

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedLease"/> class.
        /// </summary>
        /// <param name="blobStorageClientProvider">The blob storage provider that will
        /// be used to obtain the lease.</param>
        public DistributedLease(IBlobStorageClientProvider blobStorageClientProvider)
        {
            Random = new Random();
            BlobStorageClientProvider = blobStorageClientProvider;
            BlobCache = new ConcurrentDictionary<string, Task<CloudBlockBlob>>();
        }

        private string LogBaseName { get; } = "distributed_lease";

        private ConcurrentDictionary<string, Task<CloudBlockBlob>> BlobCache { get; }

        private Random Random { get; }

        private IBlobStorageClientProvider BlobStorageClientProvider { get; }

        /// <inheritdoc/>
        public Task<IDisposable> Obtain(
            string containerName,
            string name,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_obtain",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("LeaseObtainId", Guid.NewGuid())
                        .FluentAddBaseValue("LeasePreContainerName", containerName)
                        .FluentAddBaseValue("LeasePreName", name);

                    var result = default(IDisposable);
                    try
                    {
                        result = await InnerCreate(containerName, name, childLogger);

                        childLogger.FluentAddValue("LeaseAlreadyPresent", false);
                    }
                    catch (StorageException e) when (e.RequestInformation.ErrorCode == "LeaseAlreadyPresent")
                    {
                        childLogger.FluentAddValue("LeaseAlreadyPresent", true);
                    }

                    return result;
                });
        }

        /// <inheritdoc/>
        public Task<IDisposable> TryObtain(
            string containerName,
            string name,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_tryobtain",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("LeaseObtainId", Guid.NewGuid())
                        .FluentAddBaseValue("LeasePreContainerName", containerName)
                        .FluentAddBaseValue("LeasePreName", name);

                    var result = default(IDisposable);
                    var tryCount = 1;
                    while (tryCount <= 3)
                    {
                        try
                        {
                            childLogger.FluentAddValue("LeaseTryCount", tryCount);

                            return await InnerCreate(containerName, name, childLogger);
                        }
                        catch (StorageException e) when (e.RequestInformation.ErrorCode == "LeaseAlreadyPresent")
                        {
                            childLogger.FluentAddValue("LeaseAlreadyPresent", true)
                                .LogWarning($"{LogBaseName}_tryobtain_locked_complete");

                            await Task.Delay(Random.Next(500 * tryCount, 1500 * tryCount));
                        }
                    }

                    return result;
                });
        }

        private static string EnsureBlobSafeName(string name)
        {
            return name.Replace("_", string.Empty).ToLowerInvariant();
        }

        private async Task<IDisposable> InnerCreate(
            string containerName,
            string name,
            IDiagnosticsLogger logger)
        {
            var stopwatch = Stopwatch.StartNew();

            name = EnsureBlobSafeName(name);
            containerName = EnsureBlobSafeName(containerName);

            logger.FluentAddBaseValue("LeasePostContainerName", containerName)
                .FluentAddBaseValue("LeasePostName", name);

            // Create blob if needed
            var blob = await BlobCache.GetOrAdd($"{containerName}_{name}", async (x) =>
                {
                    // Setup blob details
                    var container = BlobStorageClientProvider.GetCloudBlobContainer(containerName);
                    await container.CreateIfNotExistsAsync();

                    // Get the blob
                    var newBlob = container.GetBlockBlobReference(name);
                    await newBlob.UploadTextAsync("Lock file contents. Auto-generated. Do not modify.");

                    return newBlob;
                });

            // Acquire lease
            var id = await blob.AcquireLeaseAsync(leaseTime);
            var acc = new AccessCondition() { LeaseId = id };

            var isDisposed = false;
            var timerElapsedCount = 0;

            // Trigger auto renewal
            var timer = new Timer(autoRenewLeaseTime.TotalMilliseconds);
            timer.Elapsed += async (s, e) =>
            {
                if (isDisposed)
                {
                    timer.Stop();
                    return;
                }

                await logger.OperationScopeAsync(
                    $"{LogBaseName}_auto_renew",
                    (childLogger) =>
                    {
                        logger.FluentAddDuration("LeaseTimeFromStart", stopwatch)
                            .FluentAddValue("LeasetRenewCount", timerElapsedCount += 1)
                            .FluentAddValue("LeaseRenewIsDisposed", isDisposed);

                        return blob.RenewLeaseAsync(acc);
                    },
                    swallowException: true);
            };
            timer.Start();

            // Setup disposable
            var closeCallback = ActionDisposable.Create(async () =>
            {
                timer.Stop();
                isDisposed = true;

                await logger.OperationScopeAsync(
                    $"{LogBaseName}_release",
                    (childLogger) =>
                    {
                        childLogger.FluentAddDuration("LeaseTimeFromStart", stopwatch);

                        // Force release lease
                        return blob.ReleaseLeaseAsync(acc);
                    },
                    swallowException: true);
            });

            return closeCallback;
        }
    }
}
