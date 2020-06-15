// <copyright file="CapacityManagerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Capacity manager extension.
    /// </summary>
    public static class CapacityManagerExtensions
    {
        /// <summary>
        /// Select azure resource location.
        /// </summary>
        /// <param name="capacityManager">capacity manager.</param>
        /// <param name="criteria">criteria.</param>
        /// <param name="location">location.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        public static async Task<IAzureResourceLocation> SelectAzureResourceLocation(
            this ICapacityManager capacityManager,
            IEnumerable<AzureResourceCriterion> criteria,
            AzureLocation location,
            IDiagnosticsLogger logger)
        {
            try
            {
                // Check for capacity
                return await capacityManager.SelectAzureResourceLocation(
                    criteria, location, logger.NewChildLogger());
            }
            catch (CapacityNotAvailableException ex)
            {
                // Translate to Temporarily Unavailable Exception
                throw new ContinuationTaskTemporarilyUnavailableException(
                    ex.Message, TimeSpan.FromMinutes(1), ex);
            }
        }
    }
}