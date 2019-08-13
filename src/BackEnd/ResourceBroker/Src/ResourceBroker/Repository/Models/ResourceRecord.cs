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
        public ResourceProvisioningStatus ProvisioningStatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? ProvisioningStatusChanged { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IList<ResourceProvisioningStatusChanges> ProvisioningStatusChanges { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public dynamic Properties { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newState"></param>
        public void UpdateProvisioningStatus(ResourceProvisioningStatus newState)
        {
            var time = DateTime.UtcNow;
            ProvisioningStatus = newState;
            ProvisioningStatusChanged = time;

            ProvisioningStatusChanges.Add(new ResourceProvisioningStatusChanges
            {
                Status = newState,
                Time = time,
            });
        }
    }
}
