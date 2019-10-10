using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    public enum BillingWindowBillingState
    {
        // Environment is Available and should be charged the Active
        // amount for this BillingWindow.
        Active,

        // Environment is Shutdown and should be charged the Inactive
        // amount for this BillingWindow.
        Inactive
    }
}
