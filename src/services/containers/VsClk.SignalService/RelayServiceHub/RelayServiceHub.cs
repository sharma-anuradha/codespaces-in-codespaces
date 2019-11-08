using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// The SignalR Hub class for the presence service
    /// </summary>
    public class RelayServiceHub : Hub<IRelayServiceClientHub>, IRelayServiceHub
    {
        private readonly RelayService relayService;
        private readonly ILogger logger;

        /// <summary>
        /// Hub context name when used in a SignalRHubContextHost
        /// </summary>
        public static string HubContextName = "relayServiceHub";

        public RelayServiceHub(RelayService relayService, ILogger<RelayServiceHub> logger)
        {
            this.relayService = relayService ?? throw new ArgumentNullException(nameof(relayService));
            this.logger = logger;
        }

        public Task<string> CreateHubAsync(string hubId)
        {
            return this.relayService.CreateHubAsync(hubId, Context.ConnectionAborted);
        }

        public async Task<JoinHubInfo> JoinHubAsync(string hubId, Dictionary<string, object> properties, bool createIfNotExists)
        {
            var participants = await this.relayService.JoinHubAsync(Context.ConnectionId, hubId, GetParticipantProperties(properties), createIfNotExists, Context.ConnectionAborted);

            return new JoinHubInfo()
            {
                ParticipantId = Context.ConnectionId,
                Participants = participants.Select(kvp => new HubParticipant() { Id = kvp.Key, Properties = kvp.Value }).ToArray()
            };
        }

        public Task LeaveHubAsync(string hubId)
        {
            return this.relayService.LeaveHubAsync(Context.ConnectionId, hubId, Context.ConnectionAborted);
        }

        public Task SendDataHubAsync(
            string hubId,
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data)
        {
            return this.relayService.SendDataHubAsync(Context.ConnectionId, hubId, sendOption, targetParticipantIds, type, data, Context.ConnectionAborted);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            this.logger.LogDebug($"OnDisconnectedAsync connectionId:{Context.ConnectionId}");
            await this.relayService.DisconnectAsync(Context.ConnectionId, default);
            await base.OnDisconnectedAsync(exception);
        }

        protected virtual Dictionary<string, object> GetParticipantProperties(Dictionary<string, object> properties)
        {
            return properties;
        }
    }
}
