using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace VsClk.EnvReg.Telemetry
{
    public static class DiagnosticsLoggerExtensions
    {
        const string LogValueOwnerId = "OwnerId";
        const string LogValueSessionId = "SessionId";
        const string LogValueEnvironmentId = "EnvironmentRegistrationId";
        const string LogValueComputeId = "ComputeId";
        const string LogValueComputeTargetId = "ComputeTargetId";

        public static IDiagnosticsLogger AddEnvironmentId(this IDiagnosticsLogger logger, string environmentId)
        {
            return logger.FluentAddValue(LogValueEnvironmentId, environmentId);
        }

        public static IDiagnosticsLogger AddOwnerId(this IDiagnosticsLogger logger, string ownerId)
        {
            return logger.FluentAddValue(LogValueOwnerId, ownerId);
        }

        public static IDiagnosticsLogger AddSessionId(this IDiagnosticsLogger logger, string sessionId)
        {
            return logger.FluentAddValue(LogValueSessionId, sessionId);
        }

        public static IDiagnosticsLogger AddComputeId(this IDiagnosticsLogger logger, string computeId)
        {
            return logger.FluentAddValue(LogValueComputeId, computeId);
        }

        public static IDiagnosticsLogger AddComputeTargetId(this IDiagnosticsLogger logger, string computeTargetId)
        {
            return logger.FluentAddValue(LogValueComputeTargetId, computeTargetId);
        }
    }
}
