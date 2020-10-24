// <copyright file="LogCloudEnvironmentStateJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    /// <summary>
    /// A job that will recurringly generate telemetry that logs various state information about the CloudEnvironment repository.
    /// </summary>
    public class LogCloudEnvironmentStateJobHandler : JobHandlerPayloadBase<LogCloudEnvironmentStateJobHandler.Payload>, IJobHandlerTarget
    {
        public const string LogBaseName = EnvironmentLoggingConstants.LogCloudEnvironmentsStateTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogCloudEnvironmentStateJobHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentCosmosContainer">Target Cloud Environment Cosmos DB container.</param>
        /// <param name="controlPlane">The control plan info. Used to know which AzureLocations to query over.</param>
        /// <param name="environmentMetricsLogger">The metrics logger.</param>
        public LogCloudEnvironmentStateJobHandler(
            ICloudEnvironmentCosmosContainer cloudEnvironmentCosmosContainer,
            IControlPlaneInfo controlPlane,
            IEnvironmentMetricsManager environmentMetricsLogger)
        {
            ControlPlane = Requires.NotNull(controlPlane, nameof(controlPlane));
            EnvironmentMetricsLogger = Requires.NotNull(environmentMetricsLogger, nameof(environmentMetricsLogger));
            CloudEnvironmentCosmosContainer = Requires.NotNull(cloudEnvironmentCosmosContainer, nameof(cloudEnvironmentCosmosContainer));
        }

        public IJobHandler JobHandler => this;

        public string QueueId => EnvironmentJobQueueConstants.GenericQueueName;

        public AzureLocation? Location => null;

        private IControlPlaneInfo ControlPlane { get; }

        private IEnvironmentMetricsManager EnvironmentMetricsLogger { get; }

        private ICloudEnvironmentCosmosContainer CloudEnvironmentCosmosContainer { get; }

        protected override async Task HandleJobAsync(Payload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               async (childLogger) =>
               {
                   await childLogger.OperationScopeAsync(
                        LogBaseName,
                        (itemLogger) => RunLogTaskAsync(itemLogger));
               },
               swallowException: true);
        }

        private static string PartnerString(Partner? partner) => partner.ToString() ?? "vso";

        private async Task RunLogTaskAsync(IDiagnosticsLogger childLogger)
        {
            // A batch ID incase we want to join or look at individual values
            var batchID = Guid.NewGuid();
            childLogger.AddBaseValue("BatchId", batchID.ToString());

            var total = 0;
            var countByDimensionsList = await GetCountByDimensionsForDataPlaneLocationsAsync(childLogger);

            // Send individual values
            var aggregateId = Guid.NewGuid();
            var aggregateTimestamp = DateTime.UtcNow;
            foreach (var countByDimensions in countByDimensionsList)
            {
                var count = countByDimensions.Count;
                total += count;
                childLogger.FluentAddValue("EnvironmentState", countByDimensions.State)
                           .FluentAddValue("EnvironmentSku", countByDimensions.SkuName)
                           .FluentAddValue("EnvironmentRegion", countByDimensions.Location)
                           .FluentAddValue("EnvironmentPartner", PartnerString(countByDimensions.Partner))
                           .FluentAddValue("EnvironmentCountryCode", countByDimensions.IsoCountryCode)
                           .FluentAddValue("EnvironmentAzureGeography", countByDimensions.AzureGeography)
                           .FluentAddValue("EnvironmentCount", count)
                           .LogInfo("cloud_environment_individual_measure");

                EnvironmentMetricsLogger.PostEnvironmentCount(countByDimensions, count, aggregateId, aggregateTimestamp, childLogger);
            }

            // Aggregate by State
            var byState = countByDimensionsList.GroupBy(x => x.State);
            foreach (var state in byState)
            {
                childLogger.FluentAddValue("EnvironmentState", state.Key)
                           .FluentAddValue("EnvironmentCount", state.Sum(x => x.Count))
                           .FluentAddValue("EnvironmentTotalCount", total)
                           .LogInfo("cloud_environment_state_measure");
            }

            // Aggregate by Sku
            var bySku = countByDimensionsList.GroupBy(x => x.SkuName);
            foreach (var sku in bySku)
            {
                childLogger.FluentAddValue("EnvironmentSku", sku.Key)
                           .FluentAddValue("EnvironmentCount", sku.Sum(x => x.Count))
                           .FluentAddValue("EnvironmentTotalCount", total);

                AddCountsByCloudEnvironmentState(childLogger, sku);

                childLogger.LogInfo("cloud_environment_sku_measure");
            }

            // Aggregate by Location
            var byLocation = countByDimensionsList.GroupBy(x => x.Location);
            foreach (var location in byLocation)
            {
                childLogger.FluentAddValue("EnvironmentLocation", location.Key)
                           .FluentAddValue("EnvironmentCount", location.Sum(x => x.Count))
                           .FluentAddValue("EnvironmentTotalCount", total);

                AddCountsByCloudEnvironmentState(childLogger, location);

                childLogger.LogInfo("cloud_environment_location_measure");
            }

            // Aggregate by Partner
            var byPartner = countByDimensionsList.GroupBy(x => x.Partner);
            foreach (var partner in byPartner)
            {
                childLogger.FluentAddValue("EnvironmentPartner", PartnerString(partner.Key))
                           .FluentAddValue("EnvironmentCount", partner.Sum(x => x.Count))
                           .FluentAddValue("EnvironmentTotalCount", total);

                AddCountsByCloudEnvironmentState(childLogger, partner);

                childLogger.LogInfo("cloud_environment_partner_measure");
            }

            // Aggregate by Country
            var byCountry = countByDimensionsList.GroupBy(x => x.IsoCountryCode);
            foreach (var country in byCountry)
            {
                childLogger.FluentAddValue("EnvironmentCountryCode", country.Key)
                           .FluentAddValue("EnvironmentCount", country.Sum(x => x.Count))
                           .FluentAddValue("EnvironmentTotalCount", total);

                AddCountsByCloudEnvironmentState(childLogger, country);

                childLogger.LogInfo("cloud_environment_country_measure");
            }

            // Aggregate by Geo
            var byGeo = countByDimensionsList.GroupBy(x => x.AzureGeography);
            foreach (var geo in byGeo)
            {
                childLogger.FluentAddValue("EnvironmentAzureGeography", geo.Key)
                           .FluentAddValue($"EnvironmentCount", geo.Sum(x => x.Count))
                           .FluentAddValue($"EnvironmentTotalCount", total);

                AddCountsByCloudEnvironmentState(childLogger, geo);

                childLogger.LogInfo("cloud_environment_geo_measure");
            }
        }

        private IDiagnosticsLogger AddCountsByCloudEnvironmentState<T>(IDiagnosticsLogger logger, IGrouping<T, CloudEnvironmentCountByDimensions> grouping)
        {
            foreach (CloudEnvironmentState state in Enum.GetValues(typeof(CloudEnvironmentState)))
            {
                logger.FluentAddValue($"Environment{state}Count", grouping.Where(x => x.State == state).Sum(x => x.Count));
            }

            return logger;
        }

        private async Task<IEnumerable<CloudEnvironmentCountByDimensions>> GetCountByDimensionsForDataPlaneLocationsAsync(IDiagnosticsLogger logger)
        {
            var results = new List<CloudEnvironmentCountByDimensions>();

            foreach (var location in ControlPlane.GetAllDataPlaneLocations())
            {
                var countByDimensionsForLocation = await CloudEnvironmentCosmosContainer.GetCountByDimensionsAsync(location, logger);
                results.AddRange(countByDimensionsForLocation);
            }

            return results.ToArray();
        }

        /// <summary>
        /// A log cloud environment state payload
        /// </summary>
        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : JobPayload
        {
        }
    }
}
