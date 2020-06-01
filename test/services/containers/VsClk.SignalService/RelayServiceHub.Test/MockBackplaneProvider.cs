
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public class MockBackplaneProvider : IRelayBackplaneProvider
    {
        private readonly List<OnRelayDataChangedAsync<RelayParticipantChanged>> participantChangedAsyncs = new List<OnRelayDataChangedAsync<RelayParticipantChanged>>();
        private readonly List<OnRelayDataChangedAsync<SendRelayDataHub>> sendRelayDataHubAsyncs = new List<OnRelayDataChangedAsync<SendRelayDataHub>>();
        private readonly List<OnRelayDataChangedAsync<RelayHubChanged>> relayHubChangedAsyncs = new List<OnRelayDataChangedAsync<RelayHubChanged>>();

        private readonly RelayHubManager relayHubManager = new RelayHubManager();

        public OnRelayDataChangedAsync<RelayHubChanged> RelayHubChanged
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                this.relayHubChangedAsyncs.Add(value);
            }
        }

        public OnRelayDataChangedAsync<RelayParticipantChanged> ParticipantChangedAsync 
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                this.participantChangedAsyncs.Add(value);
            }
        }

        public OnRelayDataChangedAsync<SendRelayDataHub> SendDataChangedAsync
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                this.sendRelayDataHubAsyncs.Add(value);
            }
        }

        public Task<Dictionary<string, Dictionary<string, object>>> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken)
        {
            if (relayHubManager.TryGetRelayInfo(hubId, out var relayInfo))
            {
                return Task.FromResult(relayInfo);
            }

            return Task.FromResult<Dictionary<string, Dictionary<string, object>>>(null);
        }

        public async Task SendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            await Task.WhenAll(this.sendRelayDataHubAsyncs.Select(c => c.Invoke(dataChanged, cancellationToken)));
        }

        public async Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            relayHubManager.NotifyParticipantChanged(dataChanged, out var relayHubInfo);
            await Task.WhenAll(this.participantChangedAsyncs.Select(c => c.Invoke(dataChanged, cancellationToken)));
        }

        public async Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            relayHubManager.NotifyRelayHubChanged(dataChanged);
            await Task.WhenAll(this.relayHubChangedAsyncs.Select(c => c.Invoke(dataChanged, cancellationToken)));
        }

        public Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateMetricsAsync(ServiceInfo serviceInfo, RelayServiceMetrics metrics, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool HandleException(string methodName, Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
