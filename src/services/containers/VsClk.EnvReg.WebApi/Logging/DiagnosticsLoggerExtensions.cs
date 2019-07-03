using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Diagnostics;
using VsClk.EnvReg.Telemetry;

namespace Microsoft.VsCloudKernel.Services.Logging
{
    public static class DiagnosticsLoggerExtensions
    {
        public static void AddRegistrationInfoToResponseLog(this IDiagnosticsLogger logger, EnvironmentRegistration environmentRegistration)
        {
            logger
                .AddEnvironmentId(environmentRegistration.Id)
                .AddOwnerId(environmentRegistration.OwnerId)
                .AddSessionId(environmentRegistration.Connection.ConnectionSessionId)
                .AddComputeId(environmentRegistration.Connection.ConnectionComputeId)
                .AddComputeTargetId(environmentRegistration.Connection.ConnectionComputeTargetId);
        }
    }
}
