using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public abstract class RelayBackplaneProviderTests : IAsyncLifetime
    {
        private IRelayBackplaneProvider BackplaneProvider { set; get; }

        public async Task InitializeAsync()
        {
            BackplaneProvider = await CreateBackplaneProviderAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected abstract Task<IRelayBackplaneProvider> CreateBackplaneProviderAsync();

        protected async Task TestRelayParticipantChangedInternal()
        {
            var callbackCompleted = new VisualStudio.Threading.AsyncManualResetEvent();
            RelayParticipantChanged onDataChanged = null;
            OnRelayDataChangedAsync<RelayParticipantChanged> onParticipantsChanged = (datachanged,
                cancellationToken) =>
            {
                onDataChanged = datachanged;
                callbackCompleted.Set();
                return Task.CompletedTask;
            };

            BackplaneProvider.ParticipantChangedAsync = onParticipantsChanged;
            await BackplaneProvider.NotifyParticipantChangedAsync(
                new RelayParticipantChanged("change1", "service1", "hub1", "participant1", null, ParticipantChangeType.Added),
                default);
            await callbackCompleted.WaitAsync();
            Assert.NotNull(onDataChanged);
            Assert.Equal("hub1", onDataChanged.HubId);
        }

        protected async Task TestRelayInfoInternal()
        {
            await ((IRelayBackplaneManagerProvider)BackplaneProvider).UpdateRelayHubInfo(
                "hub1",
                new Dictionary<string, Dictionary<string, object>>()
                {
                    { "participant1", new Dictionary<string, object>()
                        {
                            {  "property1", 100 }
                        }
                    },
                    { "participant2", new Dictionary<string, object>()
                        {
                            {  "property1", 200 }
                        }
                    }
                }, default);

            var relayHubInfo = await BackplaneProvider.GetRelayInfoAsync("hub1", default);
            Assert.Equal(2, relayHubInfo.Count);
            await BackplaneProvider.NotifyRelayHubChangedAsync(
                new RelayHubChanged("change1", "service1", "hub1", RelayHubChangeType.Removed),
                default);
            relayHubInfo = await BackplaneProvider.GetRelayInfoAsync("hub1", default);
            Assert.Null(relayHubInfo);
        }
    }
}
