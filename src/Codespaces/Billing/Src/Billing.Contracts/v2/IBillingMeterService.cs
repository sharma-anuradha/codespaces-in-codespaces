// <copyright file="IBillingMeterService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Interface for the IBillingMeterService.
    /// </summary>
    public interface IBillingMeterService
    {
        /// <summary>
        /// Gets usage meters based on resource usage.
        /// </summary>
        /// <param name="resourceUsageDetail">The resource usage for the plan. </param>
        /// <param name="plan">the plan.</param>
        /// <param name="end">the end time that we should base meters from.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>a dictionary of meters and the usage charges for those meters.</returns>
        IDictionary<string, double> GetUsageBasedOnResources(ResourceUsageDetail resourceUsageDetail, VsoPlanInfo plan, DateTime end, IDiagnosticsLogger logger);
    }
}
