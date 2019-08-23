// <copyright file="DistributedLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Allows compnents to obtain a distribted lease lock. This allows for distributed
    /// components to know whether another componet is working on a given resource and
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

        private ConcurrentDictionary<string, Task<CloudBlockBlob>> BlobCache { get; }

        private Random Random { get; }

        private IBlobStorageClientProvider BlobStorageClientProvider { get; }

        /// <inheritdoc/>
        public async Task<IDisposable> Obtain(
            string containerName,
            string name)
        {
            try
            {
                return await InnerCreate(containerName, name);
            }
            catch (Exception e) // TODO: Need to make sure we only catch specific exception
            {
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IDisposable> TryObtain(
            string containerName,
            string name)
        {
            var tryCount = 1;
            while (tryCount <= 3)
            {
                try
                {
                    return await InnerCreate(containerName, name);
                }
                catch (Exception e) // TODO: Need to make sure we only catch specific exception
                {
                    await Task.Delay(Random.Next(500 * tryCount, 1500 * tryCount));
                }
            }

            return null;
        }

        private static string EnsureBlobSafeName(string name)
        {
            return name.Replace("_", string.Empty).ToLowerInvariant();
        }

        private async Task<IDisposable> InnerCreate(
            string containerName,
            string name)
        {
            name = EnsureBlobSafeName(name);

            // Create blob if needed
            var blob = await BlobCache.GetOrAdd($"{containerName}_{name}", async (x) =>
                {
                    // Setup blob details
                    var container = BlobStorageClientProvider.GetCloudBlobContainer(containerName);
                    await container.CreateIfNotExistsAsync();

                    var newBlob = container.GetBlockBlobReference(name);
                    await newBlob.UploadTextAsync("test");

                    return newBlob;
                });

            // Acquire lease
            var id = await blob.AcquireLeaseAsync(leaseTime);
            var acc = new AccessCondition() { LeaseId = id };

            // Setup disposable
            var isDisposed = false;
            var closeCallback = ActionDisposable.Create(async () =>
            {
                isDisposed = true;
                try
                {
                    await blob.ReleaseLeaseAsync(acc);
                }
                catch (Exception ex)
                {
                    // TODO: need to do something here
                }
            });

            // Trigger auto renewal
            var timer = new Timer(autoRenewLeaseTime.TotalMilliseconds);
            timer.Elapsed += async (s, e) =>
            {
                if (isDisposed)
                {
                    timer.Stop();
                    return;
                }

                try
                {
                    await blob.RenewLeaseAsync(acc);
                }
                catch (Exception ex)
                {
                    // TODO: need to do something here
                }

                timer.Stop();
                timer.Start();
            };
            timer.Start();

            return closeCallback;
        }
    }
}
