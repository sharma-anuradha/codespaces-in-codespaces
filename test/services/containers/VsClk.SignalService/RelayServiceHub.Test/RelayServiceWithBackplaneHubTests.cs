
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.ServiceHubTests;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public class RelayServiceWithBackplaneHubTests : RelayServiceHubTestsBase
    {
        private readonly Dictionary<string, IClientProxy> clientProxies1;
        private readonly Dictionary<string, IClientProxy> clientProxies2;
        private readonly RelayService relayService1;
        private readonly RelayService relayService2;

        public RelayServiceWithBackplaneHubTests()
        {
            this.clientProxies1 = new Dictionary<string, IClientProxy>();
            this.clientProxies2 = new Dictionary<string, IClientProxy>();
            var serviceLogger = new Mock<ILogger<RelayService>>();
            var backplaneServiceManagerLogger = new Mock<ILogger<RelayBackplaneManager>>();

            this.relayService1 = new RelayService(
                new HubServiceOptions() { Id = "mock1" },
                MockUtils.CreateSingleHubContextHostMock<RelayServiceHub>(this.clientProxies1),
                serviceLogger.Object,
                new RelayBackplaneManager(backplaneServiceManagerLogger.Object));
            this.relayService2 = new RelayService(
                new HubServiceOptions() { Id = "mock2" },
                MockUtils.CreateSingleHubContextHostMock<RelayServiceHub>(this.clientProxies2),
                serviceLogger.Object,
                new RelayBackplaneManager(backplaneServiceManagerLogger.Object));

            var mockBackplaneProvider = new MockBackplaneProvider();
            this.relayService1.BackplaneManager.RegisterProvider(mockBackplaneProvider);
            this.relayService2.BackplaneManager.RegisterProvider(mockBackplaneProvider);
        }

        [Fact]
        public async Task Test()
        {
            await TestInternal(this.clientProxies1, this.clientProxies2, this.relayService1, this.relayService2);
        }

        private class MockBackplaneProvider : IRelayBackplaneProvider
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
                relayHubManager.NotifyParticipantChangedAsync(dataChanged, out var relayHubInfo);
                await Task.WhenAll(this.participantChangedAsyncs.Select(c => c.Invoke(dataChanged, cancellationToken)));
            }

            public async Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
            {
                relayHubManager.NotifyRelayHubChangedAsync(dataChanged);
                await Task.WhenAll(this.relayHubChangedAsyncs.Select(c => c.Invoke(dataChanged, cancellationToken)));
            }

            public Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, RelayServiceMetrics metrics, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public bool HandleException(string methodName, Exception error)
            {
                throw new NotImplementedException();
            }
        }
    }
}
