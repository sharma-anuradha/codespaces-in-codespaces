// <copyright file="EnvironmentActionValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Action Validator.
    /// </summary>
    public class EnvironmentActionValidator : IEnvironmentActionValidator
    {
        // One thousand is an arbitrary number. Have to stop somewhere
        private const string LogBaseName = "environment_actions_validator";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentActionValidator"/> class.
        /// </summary>
        /// <param name="subscriptionManager">Target subscription Manager.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        /// <param name="environmentSubscriptionManager">Target Environment Subscription Manager.</param>
        /// <param name="environmentManagerSettings">Target environment manager settings.</param>
        public EnvironmentActionValidator(
            ISubscriptionManager subscriptionManager,
            ISkuCatalog skuCatalog,
            IEnvironmentSubscriptionManager environmentSubscriptionManager,
            EnvironmentManagerSettings environmentManagerSettings)
        {
            SubscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            EnvironmentSubscriptionManager = Requires.NotNull(environmentSubscriptionManager, nameof(environmentSubscriptionManager));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
        }

        private ISubscriptionManager SubscriptionManager { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IEnvironmentSubscriptionManager EnvironmentSubscriptionManager { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <inheritdoc/>
        public async Task ValidateSubscriptionAndQuotaAsync(
            string cloudEnvironmentSkuName,
            IEnumerable<CloudEnvironment> environmentsInPlan,
            string planSubscriptionId,
            IDiagnosticsLogger logger)
        {
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironmentSkuName, out var sku);

            // Check invalid subscription
            var subscription = await SubscriptionManager.GetSubscriptionAsync(planSubscriptionId, logger.NewChildLogger());
            if (!await SubscriptionManager.CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger.NewChildLogger()))
            {
                throw new ForbiddenException((int)MessageCodes.SubscriptionCannotPerformAction);
            }

            // Check banned subscription
            if (subscription.IsBanned)
            {
                throw new ForbiddenException((int)MessageCodes.SubscriptionIsBanned);
            }

            var computeCheckEnabled = await EnvironmentManagerSettings.ComputeCheckEnabled(logger.NewChildLogger());
            var windowsComputeCheckEnabled = await EnvironmentManagerSettings.WindowsComputeCheckEnabled(logger.NewChildLogger());
            if (sku.ComputeOS == ComputeOS.Windows)
            {
                computeCheckEnabled = computeCheckEnabled && windowsComputeCheckEnabled;
            }

            if (computeCheckEnabled)
            {
                // Check subscription quota
                var reachedComputeLimit = await EnvironmentSubscriptionManager.HasReachedMaxComputeUsedForSubscriptionAsync(subscription, sku, logger.NewChildLogger());
                if (reachedComputeLimit)
                {
                    throw new ForbiddenException((int)MessageCodes.ExceededQuota);
                }
            }
            else
            {
                // Validate environment quota
                var countOfEnvironmentsInPlan = environmentsInPlan.Count();
                var maxEnvironmentsForPlan = await EnvironmentManagerSettings.MaxEnvironmentsPerPlanAsync(planSubscriptionId, logger.NewChildLogger());
                if (countOfEnvironmentsInPlan >= maxEnvironmentsForPlan)
                {
                    logger.LogError($"{LogBaseName}_create_maxenvironmentsforplan_error");
                    throw new ForbiddenException((int)MessageCodes.ExceededQuota);
                }
            }
        }
    }
}