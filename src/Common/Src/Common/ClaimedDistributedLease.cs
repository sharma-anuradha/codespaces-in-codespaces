// <copyright file="ClaimedDistributedLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
    /// either wait for it to be freed, or move on. This specific lease, only allows
    /// one lease to be obtained within a given "claim period". This supports the notion
    /// of having operations that can only happen once per day.
    /// </summary>
    public class ClaimedDistributedLease : IClaimedDistributedLease
    {
        private static TimeSpan leaseTime = TimeSpan.FromMinutes(1);
        private static TimeSpan autoRenewLeaseTime = leaseTime - TimeSpan.FromSeconds(7.5);

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimedDistributedLease"/> class.
        /// </summary>
        /// <param name="blobStorageClientProvider">Target blob storage client provider.</param>
        public ClaimedDistributedLease(IBlobStorageClientProvider blobStorageClientProvider)
        {
            BlobStorageClientProvider = blobStorageClientProvider;
        }

        private string LogBaseName { get; } = "claimed_distributed_lease";

        private IBlobStorageClientProvider BlobStorageClientProvider { get; }

        /// <inheritdoc/>
        public Task<IDisposable> Obtain(
            string containerName,
            string name,
            TimeSpan timeSpan,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_obtain",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("LeaseObtainId", Guid.NewGuid())
                        .FluentAddBaseValue("LeaseContainerName", containerName)
                        .FluentAddBaseValue("LeaseName", name)
                        .FluentAddBaseValue("LeaseClaimTimeSpan", timeSpan);

                    // If you set the schedule to be once per hour, it will round the current time, such as 5:43:20 AM,
                    // to the beginning of the hour, 5:00:00 AM. Likewise, if you set the schedule for every 20 minutes,
                    // it would change the current time of 5:43:20 AM to the start time of 5:40:00: AM.
                    var currentTime = DateTime.UtcNow;
                    var secondsClaimSpan = timeSpan.TotalSeconds;
                    var secondsSinceMidnightSpan = currentTime.TimeOfDay.TotalSeconds;
                    var secondsSinceMidnightClaimSpan = ((int)secondsSinceMidnightSpan / (int)secondsClaimSpan) * secondsClaimSpan;

                    var claimDateTime = currentTime.Date + TimeSpan.FromSeconds(secondsSinceMidnightClaimSpan);

                    try
                    {
                        return await InnerCreate(containerName, name, claimDateTime, childLogger);
                    }
                    catch (StorageException e) when (e.RequestInformation.ErrorCode == "LeaseAlreadyPresent")
                    {
                        childLogger.FluentAddValue("LeaseAlreadyPresent", true);
                    }

                    return null;
                });
        }

        private async Task<IDisposable> InnerCreate(
            string containerName,
            string name,
            DateTime claimPeriod,
            IDiagnosticsLogger logger)
        {
            var stopwatch = Stopwatch.StartNew();

            name = EnsureBlobSafeName(name);

            // Setup blob details
            var container = BlobStorageClientProvider.GetCloudBlobContainer(containerName);
            var result = await container.CreateIfNotExistsAsync();

            // Get the blob
            var blob = container.GetBlockBlobReference(name);

            // Throws if blob not found (no good way to tell if it's there in advance)
            var isFirstRun = false;
            try
            {
                // If its already been updated in this claim period, don't do anything
                if (await CheckIfAlreadyClaimed(blob, claimPeriod))
                {
                    logger.FluentAddValue("LeaseClaimLockFail", true);

                    return null;
                }
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                // Singal that this is our first run
                isFirstRun = true;

                // Continue on since this is or first time through, get the blob
                await blob.UploadTextAsync("Lock file contents. Auto-generated. Do not modify.");
            }

            logger.FluentAddValue("LeaseIsFirstRun", isFirstRun);

            // Acquire lease
            var id = await blob.AcquireLeaseAsync(leaseTime);
            var acc = new AccessCondition() { LeaseId = id };

            // Double lock check
            if (!isFirstRun && await CheckIfAlreadyClaimed(blob, claimPeriod))
            {
                logger.FluentAddValue("LeaseDoubleClaimLockFail", true);

                // Force release lease
                await blob.ReleaseLeaseAsync(acc);

                return null;
            }

            var isDisposed = false;
            var timerElapsedCount = 0;

            // Trigger auto renewal
            var timer = new Timer(autoRenewLeaseTime.TotalMilliseconds);
            timer.Elapsed += (s, e) =>
            {
                if (isDisposed)
                {
                    timer.Stop();

                    return;
                }

                logger.FluentAddDuration("LeaseTimeFromStart", stopwatch)
                    .FluentAddValue("LeasetRenewCount", timerElapsedCount += 1)
                    .FluentAddValue("LeaseRenewIsDisposed", isDisposed);

                logger.OperationScope(
                    $"{LogBaseName}_auto_renew",
                    (childLogger) => blob.RenewLeaseAsync(acc),
                    swallowException: true);
            };
            timer.Start();

            // Setup disposable
            var closeCallback = ActionDisposable.Create(() =>
            {
                timer.Stop();
                isDisposed = true;

                logger.OperationScope(
                    $"{LogBaseName}_release",
                    async (childLogger) =>
                    {
                        childLogger.FluentAddDuration("LeaseTimeFromStart", stopwatch)
                            .FluentAddValue("LeaseCurrentTime", DateTime.UtcNow);

                        // Touch file so that we can check LMT for the period logic
                        await blob.UploadTextAsync(
                            DateTime.UtcNow.ToString(),
                            new AccessCondition() { LeaseId = id },
                            new BlobRequestOptions(),
                            new OperationContext());

                        // Force release lease
                        await blob.ReleaseLeaseAsync(acc);
                    },
                    swallowException: true);
            });

            return closeCallback;
        }

        private async Task<bool> CheckIfAlreadyClaimed(
            CloudBlockBlob blob,
            DateTime claimPeriod)
        {
            // Get attributes
            await blob.FetchAttributesAsync();

            // If its already been updated in this claim period, don't do anything
            return blob.Properties.LastModified.HasValue && blob.Properties.LastModified.Value.Date >= claimPeriod;
        }

        private static string EnsureBlobSafeName(string name)
        {
            return name.Replace("_", string.Empty).ToLowerInvariant();
        }
    }
}
