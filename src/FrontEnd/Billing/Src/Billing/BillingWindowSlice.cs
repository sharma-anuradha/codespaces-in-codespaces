// <copyright file="BillingWindowSlice.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

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
        /// Gets or sets a value indicating whether the environment was Available or Shutdown.
        /// These are the only 2 states we bill for.
        /// </summary>
        public BillingWindowBillingState BillingState { get; set; }

        /// <summary>
        /// Gets or sets the Sku of the slice.
        /// </summary>
        public Sku Sku { get; set; }

        /// <summary>
        /// Gets or sets the override state for billing
        /// </summary>
        public BillingOverrideState OverrideState { get; set; }

        /// <summary>
        /// Represents environment properties which may transition between slices.  If any of these properties
        /// changes in an environment then a new slice should be created to reflect the new state.
        /// </summary>
        public class NextState
        {
            /// <summary>
            /// Gets or sets the CloudEnvironmentState the environment is transitioning to.
            /// </summary>
            public CloudEnvironmentState EnvironmentState { get; set; }

            /// <summary>
            /// Gets or sets the Sku the environment is transitioning to.
            /// </summary>
            public Sku Sku { get; set; }

            /// <summary>
            /// Gets or sets the time this state became effective.
            /// </summary>
            public DateTime TransitionTime { get; set; }
        }
    }
}
