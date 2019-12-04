// <copyright file="LogCloudEnvironmentStateTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// A task that will recurringly generate telemetry that logs various state information about the CloudEnvironment repository.
    /// </summary>
    public class LogCloudEnvironmentStateTask : ILogCloudEnvironmentStateTask
    {
        private readonly IReadOnlyDictionary<string, ICloudEnvironmentSku> SkuDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogCloudEnvironmentStateTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Environment settings used to generate the lease name</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="skuCatalog">The sku catalog (used to find which SKUs to query over.</param>
        /// <param name="controlPlane">The control plan info. Used to know which AzureLocations to query over.</param>
        /// <param name="taskHelper">The Task helper</param>
        /// <param name="claimedDistributedLease">Used to get a lease for the duration of the telemetry</param>
        /// <param name="resourceNameBuilder">Used to build the lease name</param>
        public LogCloudEnvironmentStateTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ISkuCatalog skuCatalog,
            IControlPlaneInfo controlPlane,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
        {
            EnvironmentManagerSettings = environmentManagerSettings;
            CloudEnvironmentRepository = cloudEnvironmentRepository;
            ControlPlane = controlPlane;
            SkuDictionary = skuCatalog.CloudEnvironmentSkus;
            TaskHelper = taskHelper;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(LogCloudEnvironmentStateTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.LogCloudEnvironmentsStateTask;

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IControlPlaneInfo ControlPlane { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               async (childLogger) =>
               {
                   using (var lease = await ObtainLeaseAsync(LeaseBaseName, claimSpan, childLogger))
                   {
                       childLogger.FluentAddValue("LeaseNotFound", lease == null);

                       if (lease != null)
                       {
                           await childLogger.OperationScopeAsync(
                                LogBaseName,
                                (itemLogger) => RunLogTaskAsync(itemLogger));
                       }
                   }

                   return !Disposed;
               },
               (e, _) => !Disposed,
               swallowException: true);
        }

        private async Task RunLogTaskAsync(IDiagnosticsLogger childLogger)
        {
            // A batch ID incase we want to join or look at individual values
            var batchID = Guid.NewGuid();
            childLogger.AddBaseValue("BatchId", batchID.ToString());

            var total = 0;
            IEnumerable<CloudEnvironmentLogRecord> allStates = await GetStates(childLogger);

            // Send individual values
            foreach (var record in allStates)
            {
                total += record.Count;
                childLogger.FluentAddValue($"EnvironmentState", record.State)
                           .FluentAddValue($"EnvironmentSku", record.SkuName)
                           .FluentAddValue($"EnvironmentRegion", record.Location)
                           .FluentAddValue($"EnvironmentCount", record.Count.ToString())
                           .LogInfo("cloud_environment_individual_measure");
            }

            // Aggregate by state.
            var byState = allStates.GroupBy(x => x.State);
            foreach (var state in byState)
            {
                childLogger.FluentAddValue("EnvironmentState", state.Key)
                           .FluentAddValue($"EnvironmentCount", state.Sum(x => x.Count))
                           .LogInfo("cloud_environment_state_measure");
            }

            // Aggregate by Sku
            var bySku = allStates.GroupBy(x => x.SkuName);
            foreach (var sku in bySku)
            {
                childLogger.FluentAddValue("EnvironmentSku", sku.Key)
                           .FluentAddValue($"EnvironmentCount", sku.Sum(x => x.Count))
                           .FluentAddValue($"EnvironmentActiveCount", sku.Where(x => x.State.Equals(CloudEnvironmentState.Available.ToString())).Sum(x => x.Count))
                           .FluentAddValue($"EnvironmentShutdownCount", sku.Where(x => x.State.Equals(CloudEnvironmentState.Shutdown.ToString())).Sum(x => x.Count))
                           .LogInfo("cloud_environment_sku_measure");
            }

            // Aggregate by Location
            var byLocation = allStates.GroupBy(x => x.Location);
            foreach (var location in byLocation)
            {
                childLogger.FluentAddValue("EnvironmentLocation", location.Key)
                           .FluentAddValue($"EnvironmentCount", location.Sum(x => x.Count))
                           .FluentAddValue($"EnvironmentActiveCount", location.Where(x => x.State.Equals(CloudEnvironmentState.Available.ToString())).Sum(x => x.Count))
                           .FluentAddValue($"EnvironmentShutdownCount", location.Where(x => x.State.Equals(CloudEnvironmentState.Shutdown.ToString())).Sum(x => x.Count))
                           .LogInfo("cloud_environment_location_measure");
            }
        }

        private async Task<IEnumerable<CloudEnvironmentLogRecord>> GetStates(IDiagnosticsLogger childLogger)
        {
            // TODO: If we update our SDK to 3.3 or higher, we could likely use a condensed query that'll group by all these fields.
            // This may or may not be more expensive than scalar queries though.
            // Example: SELECT c.state, c.location, c.skuName, VALUE COUNT(1) as Total FROM c GROUP BY c.state, c.location, c.skuName

            List<CloudEnvironmentLogRecord> records = new List<CloudEnvironmentLogRecord>();
            foreach (CloudEnvironmentState state in Enum.GetValues(typeof(CloudEnvironmentState)))
            {
                foreach (AzureLocation location in ControlPlane.GetAllDataPlaneLocations())
                {
                    foreach (var sku in SkuDictionary.Keys)
                    {
                        var count = await CloudEnvironmentRepository.GetCloudEnvironmentCountAsync(location.ToString(), state.ToString(), sku.ToString(), childLogger);
                        if (count > 0)
                        {
                            records.Add(new CloudEnvironmentLogRecord()
                            {
                                Count = count,
                                Location = location.ToString(),
                                SkuName = sku.ToString(),
                                State = state.ToString(),
                            });
                        }
                    }
                }
            }

            return records;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain(
                EnvironmentManagerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
