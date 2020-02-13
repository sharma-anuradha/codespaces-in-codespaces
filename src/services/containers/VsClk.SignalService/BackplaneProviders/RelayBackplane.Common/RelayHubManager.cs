// <copyright file="RelayHubManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RelayHubInfo = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Class to manage a set of relay hubs.
    /// </summary>
    public class RelayHubManager
    {
        private ConcurrentDictionary<string, RelayHubInfoHolder> RelayHubs { get; } = new ConcurrentDictionary<string, RelayHubInfoHolder>();

        public bool ContainsHub(string hubId) => RelayHubs.ContainsKey(hubId);

        public bool TryGetRelayInfo(string hubId, out RelayHubInfo relayInfo)
        {
            if (RelayHubs.TryGetValue(hubId, out var relayHub))
            {
                relayInfo = relayHub.GetRelayHubInfo();
                return true;
            }

            relayInfo = null;
            return false;
        }

        public bool NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, out RelayHubInfo relayInfo)
        {
            if (RelayHubs.TryGetValue(dataChanged.HubId, out var relayHub))
            {
                relayInfo = relayHub.Update(dataChanged);
                return true;
            }

            relayInfo = null;
            return false;
        }

        public void NotifyRelayHubChangedAsync(RelayHubChanged dataChanged)
        {
            if (dataChanged.ChangeType == RelayHubChangeType.Removed)
            {
                RelayHubs.TryRemove(dataChanged.HubId, out var relayInfo);
            }
            else
            {
                RelayHubs.TryAdd(dataChanged.HubId, new RelayHubInfoHolder());
            }
        }

        private class RelayHubInfoHolder
        {
            private readonly object lockHubInfo = new object();
            private RelayHubInfo relayHubInfo = new RelayHubInfo();

            public RelayHubInfo Update(RelayParticipantChanged dataChanged)
            {
                lock (this.lockHubInfo)
                {
                    if (dataChanged.ChangeType == ParticipantChangeType.Removed)
                    {
                        this.relayHubInfo.Remove(dataChanged.ParticipantId);
                    }
                    else
                    {
                        this.relayHubInfo[dataChanged.ParticipantId] = dataChanged.Properties;
                    }

                    return GetRelayHubInfoInternal();
                }
            }

            public RelayHubInfo GetRelayHubInfo()
            {
                lock (this.lockHubInfo)
                {
                    return GetRelayHubInfoInternal();
                }
            }

            private RelayHubInfo GetRelayHubInfoInternal()
            {
                return this.relayHubInfo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value == null ? null : kvp.Value.ToDictionary(kvp2 => kvp2.Key, kvp2 => kvp2.Value));
            }
        }
    }
}
