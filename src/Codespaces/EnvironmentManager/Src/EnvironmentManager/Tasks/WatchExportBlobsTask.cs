// <copyright file="WatchExportBlobsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper.Configuration.Conventions;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Deletes export blob and container once SAS URL has expired for the user (2 hours from creation); Updates FE Database to wipe out exported blob SAS url as well
    /// </summary>
    public class WatchExportBlobsTask : EnvironmentTaskBase, IWatchExportBlobsTask
    {
        // Number of Export storage accounts per subscription
        private const int ExportStorageAccountsPerRegionPerSubscription = 10;

        // Delete export blobs after 2 hours after it has been created
        private const int DeleteTimeHours = 2;

        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchDeletedPlanEnvironmentsTask"/> class.
        /// </summary>
        /// <param name="planRepository">The plan repository used to get deleted plans.</param>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="environmentManager">the environment manager needed to delete environments.</param>
        /// <param name="controlPlaneInfo"> The control plane info used to figure out locations to run from.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        /// <param name="azureSubscriptionCatalog">Azure subscription catalog.</param>
        /// <param name="azureClientFactory">Azure client factory.</param>
        public WatchExportBlobsTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IEnvironmentManager environmentManager,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity,
            IConfigurationReader configurationReader,
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IAzureClientFactory azureClientFactory)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, configurationReader)
        {
            EnvironmentManager = environmentManager;
            ControlPlaneInfo = controlPlaneInfo;
            CurrentIdentityProvider = currentIdentityProvider;
            SuperuserIdentity = superuserIdentity;
            AzureSubscriptionCatalog = azureSubscriptionCatalog;
            AzureClientFactory = azureClientFactory;
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchExportBlobsTask";

        private IEnvironmentManager EnvironmentManager { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IAzureClientFactory AzureClientFactory { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        private string LogBaseName => EnvironmentLoggingConstants.WatchExportBlobsTask;

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               async (childLogger) =>
               {
                   using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                   {
                       var allStamps = ControlPlaneInfo.AllStamps;

                        // Run through all stamps and export storage accounts
                       await TaskHelper.RunConcurrentEnumerableAsync(
                           $"{LogBaseName}_run_unit_check",
                           allStamps,
                           (allStamps, itemLogger) => CoreRunUnitAsync(allStamps.Key, allStamps.Value, itemLogger),
                           childLogger.NewChildLogger());

                       return !Disposed;
                   }
               },
               (e, childLogger) => Task.FromResult(!Disposed),
               swallowException: true);
        }

        private async Task CoreRunUnitAsync(AzureLocation controlPlaneLocation, IControlPlaneStampInfo controlPlaneStampInfo, IDiagnosticsLogger logger)
        {
            var stampResourceGroupName = controlPlaneStampInfo.StampInfrastructureResourceGroupName;
            var infrastructureSubscription = AzureSubscriptionCatalog.InfrastructureSubscription;

            var azure = await AzureClientFactory.GetAzureClientAsync(Guid.Parse(infrastructureSubscription.SubscriptionId));

            var infosAndLocations = ControlPlaneInfo.Stamp.DataPlaneLocations
            .SelectMany(location => Enumerable.Range(0, ExportStorageAccountsPerRegionPerSubscription)
               .Select(index => controlPlaneStampInfo.GetDataPlaneStorageAccountNameForExportStorageName(controlPlaneLocation, index))
               .Select(storageAccountName =>
           new
           {
               ResourceGroupName = stampResourceGroupName,
               StorageAccountName = storageAccountName,
           }));

            foreach (var info in infosAndLocations)
            {
                var storageAccount = azure.StorageAccounts.GetByResourceGroup(info.ResourceGroupName, info.StorageAccountName);

                await ProcessAccountAsync(storageAccount, logger.NewChildLogger());
            }
        }

        private Task<CloudStorageAccount> GetCloudStorageAccountAsync(
            IStorageAccount storageAccount,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_get_cloud_storage_account",
               async (childLogger) =>
               {
                   Requires.NotNull(storageAccount, nameof(storageAccount));

                   var storageAccountKey = storageAccount.Key;

                   Uri.TryCreate(storageAccount.EndPoints.Primary.Blob, UriKind.Absolute, out var blobEndpoint);
                   Uri.TryCreate(storageAccount.EndPoints.Primary.Queue, UriKind.Absolute, out var queueEndpoint);
                   Uri.TryCreate(storageAccount.EndPoints.Primary.Table, UriKind.Absolute, out var tableEndpoint);
                   Uri.TryCreate(storageAccount.EndPoints.Primary.File, UriKind.Absolute, out var fileEndpoint);

                   var keys = await storageAccount.GetKeysAsync();
                   var key1 = keys[0].Value;

                   var storageCreds = new StorageCredentials(storageAccount.Name, key1);
                   return new CloudStorageAccount(storageCreds, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint);
               });
        }

        private Task ProcessAccountAsync(IStorageAccount account, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_process_account",
                async (childLogger) =>
                {
                    childLogger
                        .FluentAddBaseValue("ExportAccountName", account.Name)
                        .FluentAddBaseValue("ExportTaskId", Guid.NewGuid());

                    // Get Cloud Storage Acount
                    var cloudStorageAccount = await GetCloudStorageAccountAsync(account, logger);

                    if (cloudStorageAccount != null)
                    {
                        var blobClient = cloudStorageAccount.CreateCloudBlobClient();

                        var containers = blobClient.ListContainers();

                        foreach (CloudBlobContainer container in containers)
                        {
                            var blobList = await container.ListBlobsSegmentedAsync(default);
                            var filteredBlobs = blobList.Results.OfType<CloudBlockBlob>().Where((blob) =>
                            {
                                TimeSpan ts = (TimeSpan)(DateTime.UtcNow - blob.Properties.LastModified);
                                return ts.TotalHours >= DeleteTimeHours;
                            });

                            // Each container always only contains one blob; if the blob in the container is to be deleted, filteredBlobs will be of size 1.
                            if (filteredBlobs.Count() == 1)
                            {
                                var newBlob = filteredBlobs.ElementAt(0);

                                // Logging
                                childLogger.FluentAddBaseValue("ExportBlobToDelete", newBlob.Name);
                                childLogger.FluentAddBaseValue("ExportContainerToDelete", container.Name);

                                // Parse blob name to obtain environment ID
                                var array = newBlob.Name.Split("export-");
                                var environmentId = array[1];

                                // First wipe out exported blob SAS url from DB
                                await DeleteExportBlobUrlAsync(environmentId, childLogger);

                                // Deleting the export storage blob from storage account
                                await newBlob.DeleteAsync();

                                // Deleting the container that contained export blob from storage account
                                await container.DeleteAsync();

                                // Slow down for rate limit & Database RUs
                                await Task.Delay(QueryDelay);
                            }
                        }
                    }
                });
        }

        private async Task DeleteExportBlobUrlAsync(string environmentId, IDiagnosticsLogger innerLogger)
        {
            await innerLogger.OperationScopeAsync(
                $"{LogBaseName}_delete_export_blob_url_async",
                async (childLogger)
                =>
                {
                    var parsedEnvironmentId = new Guid(environmentId);

                    var environment = await EnvironmentManager.GetAsync(parsedEnvironmentId, childLogger);

                    // Wipe out exported environment url
                    environment.ExportedBlobUrl = null;
                    await CloudEnvironmentRepository.UpdateAsync(environment, childLogger);   
                }, swallowException: true);
        }
    }
}
