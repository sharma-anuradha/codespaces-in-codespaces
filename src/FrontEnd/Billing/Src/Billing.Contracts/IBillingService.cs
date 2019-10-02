using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Interface defining the BillingWorker contract.
    /// </summary>
    public interface IBillingService
    {
        /// <summary>
        /// Generate billing summary events.
        /// </summary>
        /// <param name="logger">Logging.</param>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task GenerateBillingSummary(IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
