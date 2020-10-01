// <copyright file="EnvironmentItemAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Item Action with <see cref="CloudEnvironment"/> a default reult type.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    public abstract class EnvironmentItemAction<TInput, TState> : EnvironmentBaseItemAction<TInput, TState, CloudEnvironment>, IEnvironmentItemAction<TInput, TState>
        where TState : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentItemAction{TInput, TState}"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="skuUtils">Target sku utils.</param>
        protected EnvironmentItemAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            SkuUtils = Requires.NotNull(skuUtils, nameof(skuUtils));
        }

        /// <summary>
        /// Gets the Sku Catalog.
        /// </summary>
        protected ISkuCatalog SkuCatalog { get; }

        /// <summary>
        /// Gets the Sku Utils.
        /// </summary>
        protected ISkuUtils SkuUtils { get; }

        /// <summary>
        /// Validate sku.
        /// </summary>
        /// <param name="skuName">Target sku name.</param>
        /// <param name="plan">Target plan.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task ValidateSkuAsync(
            string skuName,
            VsoPlan plan)
        {
            var planInfo = plan.Plan;
            Requires.NotNullOrEmpty(skuName, nameof(skuName));

            SkuCatalog.CloudEnvironmentSkus.TryGetValue(skuName, out var sku);

            ValidationUtil.IsTrue(sku != null, $"The requested SKU is not defined: {skuName?.Truncate(200)}");

            if (!CurrentUserProvider.Identity.IsSuperuser())
            {
                var profile = await CurrentUserProvider.GetProfileAsync();
                var isSkuVisible = plan.Partner == Plans.Contracts.Partner.GitHub || await SkuUtils.IsVisible(sku, planInfo, profile);

                ValidationUtil.IsTrue(isSkuVisible, $"The requested SKU '{skuName?.Truncate(200)}' is not visible.");
            }

            ValidationUtil.IsTrue(sku.Enabled, $"The requested SKU '{skuName?.Truncate(200)}' is not available.");
            ValidationUtil.IsTrue(sku.SkuLocations.Contains(planInfo.Location), $"The requested SKU '{skuName?.Truncate(200)}' is not available in location: {planInfo.Location}");
        }
    }
}
