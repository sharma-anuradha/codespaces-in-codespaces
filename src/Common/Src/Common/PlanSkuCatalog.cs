// <copyright file="PlanSkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Plan sku catalog.
    /// </summary>
    public class PlanSkuCatalog : IPlanSkuCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlanSkuCatalog"/> class.
        /// </summary>
        /// <param name="planSkuCatalogOptions">The options instance for the plan sku catalog.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public PlanSkuCatalog(
            IOptions<PlanSkuCatalogOptions> planSkuCatalogOptions,
            IControlPlaneInfo controlPlaneInfo)
            : this(
                  planSkuCatalogOptions.Value.Settings,
                  controlPlaneInfo)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanSkuCatalog"/> class.
        /// </summary>
        /// <param name="planSkuCatalogSettings">The sku catalog settings.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public PlanSkuCatalog(
            PlanSkuCatalogSettings planSkuCatalogSettings,
            IControlPlaneInfo controlPlaneInfo)
        {
            Requires.NotNull(planSkuCatalogSettings, nameof(planSkuCatalogSettings));
            Requires.NotNullOrEmpty(planSkuCatalogSettings.DefaultSkuName, nameof(planSkuCatalogSettings.DefaultSkuName));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));

            DefaultSkuName = planSkuCatalogSettings.DefaultSkuName;

            var dataPlaneLocations = new HashSet<AzureLocation>(controlPlaneInfo.Stamp.DataPlaneLocations);

            foreach (var item in planSkuCatalogSettings.PlanSkuSettings)
            {
                var skuName = item.Key;
                var planSettings = item.Value;

                if (Skus.ContainsKey(skuName))
                {
                    throw new InvalidOperationException($"The plan sku '{skuName}' already exists. Is it listed more than once in appsettings.json?");
                }

                var defaultSkuConfiguration = planSkuCatalogSettings.DefaultPlanSkuConfiguration[skuName];

                // Compute the sku configuration from its own settings or from the default settings.
                var planSkuConfiguration = planSettings.PlanSkuConfiguration;
                if (planSkuConfiguration == null)
                {
                    planSkuConfiguration = defaultSkuConfiguration;
                }
                else
                {
                    // Key vault pool size setup
                    if (planSkuConfiguration.KeyVaultPoolSize.GetValueOrDefault() == default)
                    {
                        planSkuConfiguration.KeyVaultPoolSize = defaultSkuConfiguration.KeyVaultPoolSize;
                    }

                    // This is a restrictive set. Defaults might be more. Only use defaults if none set.
                    if (!planSkuConfiguration.Locations.Any())
                    {
                        planSkuConfiguration.Locations.AddRange(defaultSkuConfiguration.Locations);
                    }
                }

                // Create the ordered location list; filter to supported data-plane locations.
                var skuLocations = new ReadOnlyCollection<AzureLocation>(planSkuConfiguration.Locations
                    .Where(l => dataPlaneLocations.Contains(l))
                    .Distinct()
                    .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                    .ToList()
                    .AsReadOnly());

                var supportedFeatures = planSettings.SupportedFeatureFlags == null
                    ? Array.Empty<string>()
                    : planSettings.SupportedFeatureFlags.Distinct().ToArray();

                var enabled = planSettings.Enabled && planSkuConfiguration.Enabled;

                var keyVaultSkuName = planSettings.KeyVaultSkuName;

                var planSku = new PlanSku(
                    skuName,
                    enabled,
                    skuLocations,
                    keyVaultSkuName,
                    enabled ? planSkuConfiguration.KeyVaultPoolSize.GetValueOrDefault() : 0,
                    supportedFeatures);

                Skus.Add(skuName, planSku);
            }

            if (!Skus.ContainsKey(DefaultSkuName))
            {
                throw new Exception($"Default plan sku '{DefaultSkuName}' does not exist in the catalog.");
            }

            PlanSkus = new ReadOnlyDictionary<string, IPlanSku>(Skus);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, IPlanSku> PlanSkus { get; }

        /// <inheritdoc/>
        public string DefaultSkuName { get; }

        private IDictionary<string, IPlanSku> Skus { get; } = new Dictionary<string, IPlanSku>();
    }
}
