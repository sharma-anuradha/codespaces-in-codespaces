// <copyright file="ResourceRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceRecord : TaggedEntity
    { 
        /// <summary>
        /// 
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Subscription { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? Ready { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsAssigned { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? Assigned { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? ProvisioningStatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? ProvisioningStatusChanged { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IList<OperationStateChanges> ProvisioningStatusChanges { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? StartingStatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? StartingStatusChanged { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IList<OperationStateChanges> StartingStatusChanges { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public dynamic Properties { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newState"></param>
        public void UpdateProvisioningStatus(OperationState newState, DateTime? newTime = null)
        {
            if (ProvisioningStatus == newState)
            {
                return;
            }

            var time = newTime.GetValueOrDefault(DateTime.UtcNow);
            ProvisioningStatus = newState;
            ProvisioningStatusChanged = time;

            if (ProvisioningStatusChanges == null)
            {
                ProvisioningStatusChanges = new List<OperationStateChanges>();
            }

            // TODO: should ensure that these are bounded

            ProvisioningStatusChanges.Add(new OperationStateChanges
            {
                Status = newState,
                Time = time,
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newState"></param>
        public void UpdateStartingStatus(OperationState newState, DateTime? newTime = null)
        {
            if (StartingStatus.HasValue && StartingStatus.Value == newState)
            {
                return;
            }

            var time = newTime.GetValueOrDefault(DateTime.UtcNow);
            StartingStatus = newState;
            StartingStatusChanged = time;

            if (StartingStatusChanges == null)
            {
                StartingStatusChanges = new List<OperationStateChanges>();
            }

            // TODO: should ensure that these are bounded

            StartingStatusChanges.Add(new OperationStateChanges
            {
                Status = newState,
                Time = time,
            });
        }
    }
}
