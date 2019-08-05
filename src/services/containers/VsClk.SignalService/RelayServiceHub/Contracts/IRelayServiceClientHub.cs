using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{

    public interface IRelayServiceClientHub
    {
        Task ReceiveDataAsync(
            string hubId,
            string fromParticipantId,
            int uniqueId,
            string type,
            byte[] data);

        Task ParticipantChangedAsync(
            string hubId,
            string participantId,
            Dictionary<string, object> properties,
            ParticipantChangeType changeType);
    }
}
