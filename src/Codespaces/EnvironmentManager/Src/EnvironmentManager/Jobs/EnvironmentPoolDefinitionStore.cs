// <copyright file="EnvironmentPoolDefinitionStore.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    public class EnvironmentPoolDefinitionStore : IEnvironmentPoolDefinitionStore, IAsyncWarmup
    {
        public EnvironmentPoolDefinitionStore(ISystemCatalog systemCatalog, IControlPlaneInfo controlPlaneInfo)
        {
            SystemCatalog = Requires.NotNull(systemCatalog, nameof(systemCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        private ISystemCatalog SystemCatalog { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IList<EnvironmentPool> EnvironmentPools { get; set; }

        public Task<IList<EnvironmentPool>> RetrieveDefinitionsAsync()
        {
            return Task.FromResult(EnvironmentPools);
        }

        public void UpdateDefinitions()
        {
            // Flatten out list so we have one sku per target region
            var flatEnvironmentSkus = SystemCatalog.SkuCatalog.CloudEnvironmentSkus.Values
                .Where(s => !s.IsExternalHardware)
                .SelectMany(x => x.SkuLocations
                    .Select(y => BuildPoolDefinition(x, y)));

            EnvironmentPools = flatEnvironmentSkus.ToList();
        }

        private EnvironmentPool BuildPoolDefinition(ICloudEnvironmentSku sku, AzureLocation location)
        {
            var details = new EnvironmentPoolDetails()
            {
                Location = location,
                SkuName = sku.SkuName,
            };

            return new EnvironmentPool()
            {
                Id = details.GetPoolDefinition(),
                IsEnabled = sku.Enabled,
                TargetCount = sku.CodespacePoolLevel,
                Details = details,
            };
        }

        public Task WarmupCompletedAsync()
        {
            UpdateDefinitions();
            return Task.CompletedTask;
        }
    }
}