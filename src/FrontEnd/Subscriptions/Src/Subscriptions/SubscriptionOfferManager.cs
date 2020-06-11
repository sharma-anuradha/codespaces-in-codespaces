// <copyright file="SubscriptionOfferManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions
{
    /// <summary>
    /// The manager for all subscription offers.
    /// </summary>
    public class SubscriptionOfferManager : ISubscriptionOfferManager
    {
        /// <summary>
        /// The default compute quota limit.
        /// </summary>
        public const int DefaultComputeQuota = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionOfferManager"/> class.
        /// </summary>
        /// <param name="systemConfiguration">Used to get global system configuration values.</param>
        /// <param name="quotaFamilyCatalog">The quota family catalog.</param>
        public SubscriptionOfferManager(ISystemConfiguration systemConfiguration, IQuotaFamilyCatalog quotaFamilyCatalog)
        {
            SystemConfiguration = systemConfiguration;
            QuotaFamilyCatalog = quotaFamilyCatalog;
        }

        private ISystemConfiguration SystemConfiguration { get; }

        private IQuotaFamilyCatalog QuotaFamilyCatalog { get; }

        /// <inheritdoc />
        public async Task<int> GetComputeQuotaForOfferAsync(string quotaId, string family, IDiagnosticsLogger logger)
        {
            Requires.NotNull(quotaId, nameof(quotaId));
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var initial = -1;
            var subscriptionLimit = await SystemConfiguration.GetComputeQuotaValueAsync("quota:max-quota-for-quotaId", quotaId, family, logger, initial);

            // If there's no override check other lists.
            if (subscriptionLimit == initial)
            {
                var globalLimit = await SystemConfiguration.GetValueAsync("quota:max-compute-for-quotaId", logger, DefaultComputeQuota);
                subscriptionLimit = globalLimit;
                if (QuotaFamilyCatalog.QuotaFamilies.TryGetValue(family, out var familyLimits))
                {
                    var quotaIdLimit = familyLimits.FirstOrDefault(x => quotaId.Equals(x.Key, System.StringComparison.OrdinalIgnoreCase));
                    if (quotaIdLimit.Key != null)
                    {
                        subscriptionLimit = quotaIdLimit.Value;
                    }
                }
            }

            return subscriptionLimit;
        }
    }
}
