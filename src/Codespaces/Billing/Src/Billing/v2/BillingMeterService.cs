// <copyright file="BillingMeterService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.OData.Edm;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// The environment State change manager.
    /// </summary>
    public class BillingMeterService : IBillingMeterService
    {
        private const string LogBaseName = "billing_meter_service";

        private readonly IReadOnlyDictionary<string, ICloudEnvironmentSku> skuDictionary;
        private readonly IBillingMeterCatalog billingMeterCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingMeterService"/> class.
        /// </summary>
        /// <param name="skuCatalog">the skuCatalog.</param>
        /// <param name="billingMeterCatalog">the billing meter catalog.</param>
        public BillingMeterService(ISkuCatalog skuCatalog, IBillingMeterCatalog billingMeterCatalog)
        {
            skuDictionary = Requires.NotNull(skuCatalog.CloudEnvironmentSkus, nameof(skuCatalog.CloudEnvironmentSkus));
            this.billingMeterCatalog = Requires.NotNull(billingMeterCatalog, nameof(billingMeterCatalog));
        }

        /// <inheritdoc />
        public IDictionary<string, double> GetUsageBasedOnResources(ResourceUsageDetail resourceUsageDetail, VsoPlanInfo plan, DateTime end, IDiagnosticsLogger logger, Partner? partner = null)
        {
            var metersByCompute = billingMeterCatalog.MetersByResource.Compute;
            var metersByStorage = billingMeterCatalog.MetersByResource.Storage;

            foreach (var skuName in skuDictionary.Keys)
            {
                var skuComputeNameGroups = resourceUsageDetail.Compute.Where(t => t.Sku == skuName);
                var skuStorageNameGroups = resourceUsageDetail.Storage.Where(t => t.Sku == skuName);

                // Find the meters that apply to this sku, region.
                // There should only 1 meter per combination per resource type
                var computeMeter = metersByCompute.FirstOrDefault(t => t.SkuName == skuName && t.Region == plan.Location);
                var storageMeter = metersByStorage.FirstOrDefault(t => t.SkuName == skuName && t.Region == plan.Location);

                if (computeMeter != default && computeMeter.EnabledOnDate.Date < end &&
                    storageMeter != default && storageMeter.EnabledOnDate.Date < end &&
                    partner != null && partner == Partner.GitHub)
                {
                    return GetUsage(resourceUsageDetail, computeMeter.MeterId, storageMeter.MeterId, logger);
                }
                else
                {
                    // use legacy meter
                    return GetLegacyUsage(resourceUsageDetail, plan, end, logger);
                }
            }

            // No Skus present in skuDictionary
            logger.LogError("BillingWorker_EmptySkuDictionary");
            return new Dictionary<string, double>();
        }

        private IDictionary<string, double> GetUsage(ResourceUsageDetail resourceUsageDetail, string computeMeter, string storageMeter, IDiagnosticsLogger logger)
        {
            Dictionary<string, double> usageByMeter = new Dictionary<string, double>();

            foreach (var storageResource in resourceUsageDetail.Storage)
            {
                var secondsPerMonth = 730 * 3600; // Azure seconds per Month.
                var gbSeconds = storageResource.Size * storageResource.Usage; // GB Seconds Used (i.e. 64GB * 3600 seconds)
                var gbMonths = gbSeconds / secondsPerMonth;
                if (usageByMeter.ContainsKey(computeMeter))
                {
                    usageByMeter[computeMeter] += gbMonths;
                }
                else
                {
                    usageByMeter.Add(computeMeter, gbMonths);
                }
            }

            foreach (var computeResource in resourceUsageDetail.Compute)
            {
                var hoursUsed = computeResource.Usage / 3600;
                if (usageByMeter.ContainsKey(storageMeter))
                {
                    usageByMeter[storageMeter] += hoursUsed;
                }
                else
                {
                    usageByMeter.Add(storageMeter, hoursUsed);
                }
            }

            return usageByMeter;
        }

        private IDictionary<string, double> GetLegacyUsage(ResourceUsageDetail resourceUsageDetail, VsoPlanInfo plan, DateTime end, IDiagnosticsLogger logger)
        {
            var meterId = GetMeterId(plan.Location, BillingResourceType.Blended, end, string.Empty, logger);
            var billableUnits = CalculateVsoUnitsByTimeAndSku(resourceUsageDetail, logger);

            return new Dictionary<string, double>() { [meterId] = billableUnits };
        }

        private double CalculateVsoUnitsByTimeAndSku(ResourceUsageDetail usageDetail, IDiagnosticsLogger logger)
        {
            double billableUnits = 0;

            // calculate compute
            foreach (var detail in usageDetail.Compute)
            {
                if (skuDictionary.TryGetValue(detail.Sku, out var sku))
                {
                    billableUnits += GetBillableUnits((double)sku.ComputeVsoUnitsPerHour, detail.Usage);
                }
                else
                {
                    logger.FluentAddValue("Sku", detail.Sku);
                    logger.LogError($"{LogBaseName}_invalid_sku");
                }
            }

            foreach (var detail in usageDetail.Storage)
            {
                if (skuDictionary.TryGetValue(detail.Sku, out var sku))
                {
                    billableUnits += GetBillableUnits((double)sku.StorageVsoUnitsPerHour, detail.Usage);
                }
                else
                {
                    logger.FluentAddValue("Sku", detail.Sku);
                    logger.LogError($"{LogBaseName}_invalid_sku");
                }
            }

            return billableUnits;
        }

        private double GetBillableUnits(double unitsPerHour, double usageInSeconds)
        {
            const double secondsPerHour = 3600;
            var unitsPerSecond = unitsPerHour / secondsPerHour;
            return usageInSeconds * unitsPerSecond;
        }

        private string GetMeterId(AzureLocation location, BillingResourceType resourceType, DateTime date, string sku, IDiagnosticsLogger logger)
        {
            if (billingMeterCatalog.MetersByLocation.TryGetValue(location, out var id))
            {
                return id;
            }
            else
            {
                logger.FluentAddValue("Azurelocation", location.ToString());
                logger.LogError("BillingWorker_GetMeterID_UnsupportedLocation");
                return "METER_NOT_FOUND";
            }
        }
    }
}
