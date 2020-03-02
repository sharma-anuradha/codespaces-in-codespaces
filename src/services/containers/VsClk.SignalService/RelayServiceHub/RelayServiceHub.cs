// <copyright file="RelayServiceHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// The SignalR Hub class for the presence service.
    /// </summary>
    public class RelayServiceHub : Hub<IRelayServiceClientHub>, IRelayServiceHub
    {
        private readonly RelayService relayService;
        private readonly ILogger logger;

        public RelayServiceHub(RelayService relayService, ILogger<RelayServiceHub> logger)
        {
            this.relayService = relayService ?? throw new ArgumentNullException(nameof(relayService));
            this.logger = logger;
        }

        /// <summary>
        /// Hub context name when used in a SignalRHubContextHost.
        /// </summary>
        public static string HubContextName => "relayServiceHub";

        public Task<string> CreateHubAsync(string hubId)
        {
            return this.relayService.CreateHubAsync(hubId, Context.ConnectionAborted);
        }

        public Task DeleteHubAsync(string hubId)
        {
            return this.relayService.DeleteHubAsync(hubId, Context.ConnectionAborted);
        }

        public async Task<JoinHubInfo> JoinHubAsync(string hubId, Dictionary<string, object> properties, JoinOptions joinOptions)
        {
            var participants = await this.relayService.JoinHubAsync(Context.ConnectionId, hubId, GetParticipantProperties(properties), joinOptions, Context.ConnectionAborted);

            return new JoinHubInfo()
            {
                ServiceId = this.relayService.Options.Id,
                Stamp = this.relayService.Options.Stamp,
                ParticipantId = Context.ConnectionId,
                Participants = participants.Select(kvp => new HubParticipant()
                {
                    Id = kvp.Key,
                    Properties = kvp.Value,
                }).ToArray(),
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

        public Task SendDataHubExAsync(SendHubData sendHubData, object[] data)
        {
            var dataArray = Array.ConvertAll(data, b => Convert.ToByte(b));
            return this.relayService.SendDataHubAsync(
                Context.ConnectionId,
                sendHubData.HubId,
                (SendOption)sendHubData.SendOption,
                sendHubData.TargetParticipantIds,
                sendHubData.Type,
                dataArray,
                Context.ConnectionAborted);
        }

        public Task UpdateAsync(string hubId, Dictionary<string, object> properties)
        {
            return this.relayService.UpdateAsync(Context.ConnectionId, hubId, properties, Context.ConnectionAborted);
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
