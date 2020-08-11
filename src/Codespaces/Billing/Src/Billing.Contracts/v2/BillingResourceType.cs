// <copyright file="BillingResourceType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// BillingResourceTypes.
    /// </summary>
    public enum BillingResourceType
    {
        /// <summary>
        /// Compute and storage billed together.
        /// </summary>
        Blended = 0,

        /// <summary>
        /// Compute billing resource.
        /// </summary>
        Compute = 1,

        /// <summary>
        /// Compute billing resource.
        /// </summary>
        Storage = 2,
    }
}
