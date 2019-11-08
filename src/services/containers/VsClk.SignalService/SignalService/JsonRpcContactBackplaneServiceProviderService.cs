using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Long running service that will create a jsonRpc backplane provider connected to the backplane service hub
    /// </summary>
    public class JsonRpcContactBackplaneServiceProviderService : WarmupServiceBase
    {
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly ILogger<JsonRpcContactBackplaneServiceProvider> logger;
        private readonly IContactBackplaneManager backplaneManager;
        private readonly IDataFormatProvider serviceFormatProvider;
        private readonly IStartupBase startup;

        public JsonRpcContactBackplaneServiceProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            IContactBackplaneManager backplaneManager,
            ILogger<JsonRpcContactBackplaneServiceProvider> logger,
            IStartupBase startup,
            IDataFormatProvider serviceFormatProvider = null)
            : base(warmupServices, healthStatusProviders)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.logger = logger;
            this.backplaneManager = backplaneManager;
            this.serviceFormatProvider = serviceFormatProvider;
            this.startup = startup;
        }

        private AppSettings AppSettings => this.appSettingsProvider.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // this call will not return until a successfully connection to the backplane service hub is completed
            var backplaneProvider = await JsonRpcContactBackplaneServiceProvider.CreateAsync(
                AppSettings.BackplaneJsonRpcServer,
                this.logger,
                this.startup.ServiceId,
                stoppingToken);
            // register the provider
            this.backplaneManager.RegisterProvider(backplaneProvider);
            CompleteWarmup(true);
        }
    }
}
