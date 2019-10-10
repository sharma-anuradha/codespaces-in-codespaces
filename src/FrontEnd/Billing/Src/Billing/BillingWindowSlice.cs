// <copyright file="BillingWindowSlice.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Represents a slice of billable VSO Environment usage.
    /// </summary>
    public class BillingWindowSlice
    {
        /// <summary>
        /// Gets or sets the DatTime start of the slice.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the DateTime end of the slice.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the last CloudEnvironmentState of the slice.
        /// </summary>
        public CloudEnvironmentState LastState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment was Available or Shutdown.
        /// These are the only 2 states we bill for.
        /// </summary>
        public BillingWindowBillingState BillingState { get; set; }
    }
}
