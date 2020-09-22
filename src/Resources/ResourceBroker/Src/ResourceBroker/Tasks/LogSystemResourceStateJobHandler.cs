// <copyright file="LogSystemResourceStateJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    public class LogSystemResourceStateJobHandler : JobHandlerPayloadBase<LogSystemResourceStateJobProducer.LogSystemResourceStatePayload>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogSystemResourceStateJobHandler"/> class.
        /// <param name="resourceRepository">Resource Repository.</param>
        /// </summary>
        public LogSystemResourceStateJobHandler(IResourceRepository resourceRepository)
        {
            ResourceRepository = resourceRepository;     
        }

        private string LogBaseName => ResourceLoggingConstants.LogSystemResourceStateJobHandler;

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(LogSystemResourceStateJobProducer.LogSystemResourceStatePayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    await childLogger.OperationScopeAsync(
                        LogBaseName,
                        (itemLogger) => LogSystemResourceStateAsync(itemLogger));         
                },
                swallowException: true);
        }

        private async Task LogSystemResourceStateAsync(IDiagnosticsLogger childLogger)
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
    }
}
