// <copyright file="JsonRpcBackplaneServiceProviderService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Long running service that will create a jsonRpc backplane provider connected to the backplane service hub.
    /// </summary>
    public class JsonRpcBackplaneServiceProviderService<TBackplaneManagerType, TBackplaneServiceProviderType> : WarmupServiceBase
        where TBackplaneServiceProviderType : BackplaneServiceProviderBase
    {
        private const int Port = 3150;
        private const string RegisterProviderMethod = "RegisterProvider";

        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly ILogger<JsonRpcConnectorProvider> logger;
        private readonly TBackplaneManagerType backplaneManager;
        private readonly IStartupBase startup;

        public JsonRpcBackplaneServiceProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            TBackplaneManagerType backplaneManager,
            ILogger<JsonRpcConnectorProvider> logger,
            IStartupBase startup)
            : base(warmupServices, healthStatusProviders)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.logger = logger;
            this.backplaneManager = backplaneManager;
            this.startup = startup;
        }

        private AppSettings AppSettings => this.appSettingsProvider.Value;

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var backplaneConnectorProvider = new JsonRpcConnectorProvider(
                AppSettings.BackplaneHostName,
                Port,
                this.logger);
            var backplaneProviderService = Activator.CreateInstance(
                typeof(TBackplaneServiceProviderType),
                backplaneConnectorProvider,
                this.startup.ServiceId,
                stoppingToken) as BackplaneServiceProviderBase;
            await backplaneProviderService.AttemptConnectAsync(stoppingToken);

            // register the provider
            backplaneManager.GetType().GetMethod(RegisterProviderMethod).Invoke(
                this.backplaneManager,
                new object[] { backplaneProviderService, null });

            CompleteWarmup(true);
        }
    }
}
