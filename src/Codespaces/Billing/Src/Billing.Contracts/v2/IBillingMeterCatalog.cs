// <copyright file="IBillingMeterCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Contract representing the billing meter catelog.
    /// </summary>
    public interface IBillingMeterCatalog
    {
        /// <summary>
        /// Gets the legacy meters by location dictionary.
        /// </summary>
        IReadOnlyDictionary<AzureLocation, string> MetersByLocation { get; }

        /// <summary>
        /// Gets the meters by resource type lists.
        /// </summary>
        ResourceBillingMeters MetersByResource { get; }
    }
}
