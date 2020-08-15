// <copyright file="BillingUtilities.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A set of utility methods for billing.
    /// </summary>
    public class BillingUtilities
    {
        /// <summary>
        /// Gets the last billable state change for a environment.
        /// </summary>
        /// <param name="envEventsSinceStart">All the events for a particular environment.</param>
        /// <returns>the most recent change that counts a a billable state.</returns>
        public static EnvironmentStateChange GetLastBillableEventStateChange(IEnumerable<EnvironmentStateChange> envEventsSinceStart)
        {
            EnvironmentStateChange lastState = null;
            foreach (var billEvent in envEventsSinceStart.OrderBy(x => x.Time))
            {
                var newValue = billEvent.NewValue;

                if (newValue.Equals(nameof(CloudEnvironmentState.Deleted)) ||
                    newValue.Equals(nameof(CloudEnvironmentState.Moved)))
                {
                    return billEvent; // The environment was deleted. We are done and shouldn't consider other states. Deleted is the terminal state.
                }

                if (newValue.Equals(nameof(CloudEnvironmentState.Available)) ||
                    newValue.Equals(nameof(CloudEnvironmentState.Shutdown)) ||
                    newValue.Equals(nameof(CloudEnvironmentState.Archived)))
                {
                    lastState = billEvent;
                }
            }

            return lastState;
        }
    }
}
