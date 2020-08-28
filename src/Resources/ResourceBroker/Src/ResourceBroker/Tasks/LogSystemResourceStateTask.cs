// <copyright file="LogSystemResourceStateTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    public class LogSystemResourceStateTask : ILogSystemResourceStateTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogSystemResourceStateTask"/> class.
        /// </summary>
        public LogSystemResourceStateTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ResourceRepository = resourceRepository;
            ResourcePoolDefinitionStore = resourcePoolDefinitionStore;
            TaskHelper = taskHelper;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
            ControlPlaneInfo = controlPlaneInfo;
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(LogSystemResourceStateTask)}Lease");

        private string LogBaseName => ResourceLoggingConstants.LogSystemResourceStateTask;

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IResourceRepository ResourceRepository { get; }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    using (var lease = ObtainLease($"{LeaseBaseName}-{ControlPlaneInfo.Stamp.Location}", claimSpan, childLogger))
                    {
                        childLogger.FluentAddValue("LeaseNotFound", lease == null);

                        if (lease != null)
                        {
                            await childLogger.OperationScopeAsync(
                                 LogBaseName,
                                 (itemLogger) => CoreRunUnitAsync(itemLogger));
                        }
                    }

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task CoreRunUnitAsync(IDiagnosticsLogger childLogger)
        {
            // A batch ID incase we want to join or look at individual values
            var batchID = Guid.NewGuid();
            childLogger.FluentAddBaseValue("BatchId", batchID.ToString());

            await childLogger.OperationScopeAsync(
                $"{LogBaseName}_run_resources",
                async (resourcesLogger) =>
                {
                    var resourceCountsDimensionsList = await ResourceRepository.GetResourceCountByDimensionsAsync(resourcesLogger.NewChildLogger());

                    resourcesLogger.FluentAddBaseValue("ResourceIsComponent", false);
                    ProcessResourceCounts(resourceCountsDimensionsList.ToArray(), resourcesLogger);
                });

            await childLogger.OperationScopeAsync(
                $"{LogBaseName}_run_components",
                async (componentsLogger) =>
                {
                    var componentCountsDimensionsList = await ResourceRepository.GetComponentCountByDimensionsAsync(componentsLogger.NewChildLogger());

                    componentsLogger.FluentAddBaseValue("ResourceIsComponent", true);
                    ProcessResourceCounts(componentCountsDimensionsList.ToArray(), componentsLogger);
                });
        }

        private void ProcessResourceCounts(IEnumerable<SystemResourceCountByDimensions> counts, IDiagnosticsLogger childLogger)
        {
            var total = 0;

            // Send individual values            
            foreach (var countByDimensions in counts)
            {
                var count = countByDimensions.Count;
                total += count;
                childLogger.FluentAddValue("ResourceIsReady", countByDimensions.IsReady)
                           .FluentAddValue("ResourceIsAssigned", countByDimensions.IsAssigned)
                           .FluentAddValue("ResourceType", countByDimensions.Type)
                           .FluentAddValue("ResourceSkuName", countByDimensions.SkuName)
                           .FluentAddValue("ResourceLocation", countByDimensions.Location)
                           .FluentAddValue("ResourcePoolReferenceCode", countByDimensions.PoolReferenceCode)
                           .FluentAddValue("ResourceSubscriptionId", countByDimensions.SubscriptionId)
                           .FluentAddValue("ResourceCount", count)
                           .LogInfo("system_resource_individual_measure");
            }

            // Aggregate by Type
            var byType = counts.GroupBy(x => x.Type);
            foreach (var item in byType)
            {
                var sample = item.First();
                childLogger.FluentAddValue("ResourceType", sample.Type)
                           .FluentAddValue("ResourceLocation", sample.Location)
                           .FluentAddValue("ResourceCount", item.Sum(x => x.Count))
                           .FluentAddValue("ResourceTotalCount", total)
                           .LogInfo("system_resource_type_measure");
            }

            // Aggregate by Sku
            var bySku = counts.GroupBy(x => $"{x.SkuName}_{x.Type}");
            foreach (var item in bySku)
            {
                var sample = item.First();
                childLogger.FluentAddValue("ResourceType", sample.Type)
                           .FluentAddValue("ResourceSkuName", sample.SkuName)
                           .FluentAddValue("ResourceLocation", sample.Location)
                           .FluentAddValue("ResourceCount", item.Sum(x => x.Count))
                           .FluentAddValue("ResourceTotalCount", total)
                           .LogInfo("system_resource_skuname_measure");
            }

            // Aggregate by IsReady
            var byIsReady = counts.GroupBy(x => $"{x.SkuName}_{x.Type}_{x.IsReady}");
            foreach (var item in byIsReady)
            {
                var sample = item.First();
                childLogger.FluentAddValue("ResourceType", sample.Type)
                           .FluentAddValue("ResourceSkuName", sample.SkuName)
                           .FluentAddValue("ResourceIsReady", sample.IsReady)
                           .FluentAddValue("ResourceLocation", sample.Location)
                           .FluentAddValue("ResourceCount", item.Sum(x => x.Count))
                           .FluentAddValue("ResourceTotalCount", total)
                           .LogInfo("system_resource_isready_measure");
            }

            // Aggregate by IsAssigned
            var byIsAssigned = counts.GroupBy(x => $"{x.SkuName}_{x.Type}_{x.IsReady}_{x.IsAssigned}");
            foreach (var item in byIsAssigned)
            {
                var sample = item.First();
                childLogger.FluentAddValue("ResourceType", sample.Type)
                           .FluentAddValue("ResourceSkuName", sample.SkuName)
                           .FluentAddValue("ResourceIsReady", sample.IsReady)
                           .FluentAddValue("ResourceIsAssigned", sample.IsAssigned)
                           .FluentAddValue("ResourceLocation", sample.Location)
                           .FluentAddValue("ResourceCount", item.Sum(x => x.Count))
                           .FluentAddValue("ResourceTotalCount", total)
                           .LogInfo("system_resource_isassigned_measure");
            }
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
