using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HeartBeat.Test
{ 
    public class MonitoringTestBase
    {
        public readonly IDiagnosticsLoggerFactory loggerFactory;
        public readonly IDiagnosticsLogger logger;
        public readonly ICloudEnvironmentManager cloudEnvironmentManager;
        public readonly EnvironmentSessionDataHandler environmentSessionDataHandler;

        public MonitoringTestBase()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
            cloudEnvironmentManager = new MockCloudEnvironmentManager();
            environmentSessionDataHandler = new EnvironmentSessionDataHandler(cloudEnvironmentManager);
        }

        protected async void UpdateCloudEnvironmentForTest(CloudEnvironment cloudEnvironment)
        {
            await cloudEnvironmentManager.UpdateAsync(cloudEnvironment, CloudEnvironmentState.None, string.Empty, string.Empty, logger);
        }

        protected async Task<CloudEnvironment> GetCloudEnvironment()
        {
            return await cloudEnvironmentManager.GetAsync(string.Empty, logger);
        }

        public static readonly EnvironmentData testEnvironmentData = new EnvironmentData
        {
            EnvironmentId = "1",
        };

        public static readonly Guid vmResourceId = Guid.NewGuid();
    }
}
