using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Background Worker for monitoring plans.
    /// </summary>
    public class PlanWorker : BackgroundService
    {
        private static readonly TimeSpan PlanCountRefreshInterval = TimeSpan.FromMinutes(1);

        private readonly IPlanManager planManager;
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanWorker"/> class.
        /// </summary>
        /// <param name="planManager">IPlanManager.</param>
        /// <param name="diagnosticsLogger">IDiagnosticLogger.</param>
        public PlanWorker(IPlanManager planManager, IDiagnosticsLogger diagnosticsLogger)
        {
            this.planManager = planManager;
            this.logger = diagnosticsLogger.NewChildLogger();
        }

        /// <summary>
        /// Executes a background Task.
        /// </summary>
        /// <param name="cancellationToken">Notification object for stopping the task.</param>
        /// <returns>Task.</returns>
        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.FluentAddBaseValue("Service", "planservices");
            logger.LogInfo("Plan Worker is initializing.");
            cancellationToken.Register(() => logger.LogInfo("Plan Worker was cancelled."));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var duration = logger.StartDuration();

                    await planManager.RefreshTotalPlansCountAsync(logger);

                    logger.AddDuration(duration).LogInfo(GetType().FormatLogMessage(nameof(ExecuteAsync)));

                    await Task.Delay(PlanCountRefreshInterval - duration.Elapsed, cancellationToken);
                }
                catch (OperationCanceledException oce)
                when (cancellationToken.Equals(oce.CancellationToken))
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogErrorWithDetail("Error executing Plan Worker.", ex.Message);
                }
            }

            logger.LogInfo("Plan Worker is stopping.");
        }
    }
}
