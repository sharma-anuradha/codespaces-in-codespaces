using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HeartBeat.Test
{
    public class MonitoringTestBase
    {
        public readonly IDiagnosticsLoggerFactory loggerFactory;
        public readonly IDiagnosticsLogger logger;
        public readonly IEnvironmentManager environmentManager;
        public readonly ILatestHeartbeatMonitor latestHeartbeatMonitor;
        public readonly ICloudEnvironmentHeartbeatRepository heartbeatRepository;
        public readonly EnvironmentSessionDataHandler environmentSessionDataHandler;

        public MonitoringTestBase()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
            heartbeatRepository = new MockCloudEnvironmentHeartbeatRepository();
            environmentManager = new MockEnvironmentManager();
            latestHeartbeatMonitor = new LatestHeartbeatMonitor();
            environmentSessionDataHandler = new EnvironmentSessionDataHandler();
        }

        protected async void UpdateCloudEnvironmentForTest(CloudEnvironment cloudEnvironment)
        {
            await environmentManager.UpdateAsync(cloudEnvironment, CloudEnvironmentState.None, string.Empty, string.Empty, null, logger);
        }

        protected async Task<CloudEnvironment> GetCloudEnvironment()
        {
            return await environmentManager.GetAsync(Guid.Empty, logger);
        }

        protected async Task<CollectedDataHandlerContext> GetHandlerContext()
        {
            var cloudEnvironment = new EnvironmentTransition(await GetCloudEnvironment());

            return new CollectedDataHandlerContext(cloudEnvironment);
        }

        public static readonly EnvironmentData testEnvironmentData = new EnvironmentData
        {
            EnvironmentId = "1",
        };

        public static readonly Guid vmResourceId = Guid.NewGuid();
    }
}
