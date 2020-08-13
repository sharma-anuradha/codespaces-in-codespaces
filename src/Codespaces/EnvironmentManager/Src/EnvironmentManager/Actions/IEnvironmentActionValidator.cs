// <copyright file="IEnvironmentActionValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Action Validator.
    /// </summary>
    public interface IEnvironmentActionValidator
    {
        /// <summary>
        /// Validate subscription and plan for create and restore action.
        /// </summary>
        /// <param name="cloudEnvironmentSkuName">Cloud environment Sku Name.</param>
        /// <param name="environmentsInPlan">Existing active environments in the plan.</param>
        /// <param name="planSubscriptionId">Plan subscripton ID.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The async task with result SubscriptionComputeData object.</returns>
        public Task<SubscriptionComputeData> ValidateSubscriptionAndQuotaAsync(
            string cloudEnvironmentSkuName,
            IEnumerable<CloudEnvironment> environmentsInPlan,
            string planSubscriptionId,
            IDiagnosticsLogger logger);
    }
}