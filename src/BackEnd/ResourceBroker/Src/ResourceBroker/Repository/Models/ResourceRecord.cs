// <copyright file="ResourceRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource record.
    /// </summary>
    public class ResourceRecord : TaggedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRecord"/> class.
        /// </summary>
        public ResourceRecord()
        {
            KeepAlives = new ResourceKeepAliveRecord();
            Properties = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the resource sku name.
        /// </summary>
        [JsonProperty(PropertyName = "skuName")]
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "type")]
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the azure resource info from the compute or storage provider.
        /// </summary>
        [JsonProperty(PropertyName = "azureResourceInfo")]
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the azure location.
        /// </summary>
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is ready.
        /// </summary>
        [JsonProperty(PropertyName = "isReady")]
        public bool IsReady { get; set; }

        /// <summary>
        /// Gets or sets the ready date.
        /// </summary>
        [JsonProperty(PropertyName = "ready")]
        public DateTime? Ready { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is assigned.
        /// </summary>
        [JsonProperty(PropertyName = "isAssigned")]
        public bool IsAssigned { get; set; }

        /// <summary>
        /// Gets or sets the assigned date.
        /// </summary>
        [JsonProperty(PropertyName = "assigned")]
        public DateTime? Assigned { get; set; }

        /// <summary>
        /// Gets or sets the created date.
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the pool reference details.
        /// </summary>
        [JsonProperty(PropertyName = "poolReference")]
        public ResourcePoolDefinitionRecord PoolReference { get; set; }

        /// <summary>
        /// Gets or sets the provisioning reason.
        /// </summary>
        [JsonProperty(PropertyName = "provisioningReason")]
        public string ProvisioningReason { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status.
        /// </summary>
        [JsonProperty(PropertyName = "provisioningStatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? ProvisioningStatus { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status Changed date.
        /// </summary>
        [JsonProperty(PropertyName = "provisioningStatusChanged")]
        public DateTime? ProvisioningStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status Changes.
        /// </summary>
        [JsonProperty(PropertyName = "provisioningStatusChanges")]
        public IList<OperationStateChanges> ProvisioningStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the starting reason.
        /// </summary>
        [JsonProperty(PropertyName = "startingReason")]
        public string StartingReason { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status.
        /// </summary>
        [JsonProperty(PropertyName = "startingStatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? StartingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status Changed date.
        /// </summary>
        [JsonProperty(PropertyName = "startingStatusChanged")]
        public DateTime? StartingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status Changes.
        /// </summary>
        [JsonProperty(PropertyName = "startingStatusChanges")]
        public IList<OperationStateChanges> StartingStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is deleted.
        /// </summary>
        [JsonProperty(PropertyName = "isDeleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the count of how many times we have tried to delete the resource.
        /// </summary>
        [JsonProperty(PropertyName = "deleteAttemptCount")]
        public int DeleteAttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the starting reason.
        /// </summary>
        [JsonProperty(PropertyName = "deletingReason")]
        public string DeletingReason { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status.
        /// </summary>
        [JsonProperty(PropertyName = "deletingStatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? DeletingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status Changed date.
        /// </summary>
        [JsonProperty(PropertyName = "deletingStatusChanged")]
        public DateTime? DeletingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status Changes.
        /// </summary>
        [JsonProperty(PropertyName = "deletingStatusChanges")]
        public IList<OperationStateChanges> DeletingStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the current state of the Keep Alives.
        /// </summary>
        [JsonProperty(PropertyName = "keepAlives")]
        public ResourceKeepAliveRecord KeepAlives { get; set; }

        /// <summary>
        /// Gets or sets the cleanup reason.
        /// </summary>
        [JsonProperty(PropertyName = "cleanupReason")]
        public string CleanupReason { get; set; }

        /// <summary>
        /// Gets or sets the cleanup status.
        /// </summary>
        [JsonProperty(PropertyName = "cleanupStatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? CleanupStatus { get; set; }

        /// <summary>
        /// Gets or sets the cleanup Status Changed date.
        /// </summary>
        [JsonProperty(PropertyName = "cleanupStatusChanged")]
        public DateTime? CleanupStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the cleanup Status Changes.
        /// </summary>
        [JsonProperty(PropertyName = "cleanupStatusChanges")]
        public IList<OperationStateChanges> CleanupStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the current HeartBeat.
        /// </summary>
        [JsonProperty(PropertyName = "heartBeatSummary")]
        public ResourceHeartBeatSummaryRecord HeartBeatSummary { get; set; }

        /// <summary>
        /// Gets or sets the Properties.
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Build common stub of new record.
        /// </summary>
        /// <param name="id">Target id.</param>
        /// <param name="time">Target time.</param>
        /// <param name="type">Target type.</param>
        /// <param name="locaiton">Targert location.</param>
        /// <param name="skuName">Target sku.</param>
        /// <param name="poolReference">Target pool reference.</param>
        /// <param name="properties">Target properties.</param>
        /// <returns>Stub resource record.</returns>
        public static ResourceRecord Build(
            Guid id,
            DateTime time,
            ResourceType type,
            AzureLocation locaiton,
            string skuName = null,
            ResourcePoolDefinitionRecord poolReference = null,
            IDictionary<string, string> properties = null)
        {
            var stub = new ResourceRecord
                {
                    Id = id.ToString(),
                    Type = type,
                    IsReady = false,
                    Ready = null,
                    IsAssigned = false,
                    Assigned = null,
                    Created = time,
                    Location = locaiton.ToString().ToLowerInvariant(),
                    SkuName = skuName,
                    PoolReference = poolReference,
                };

            if (properties != null)
            {
                stub.Properties = properties;
            }

            return stub;
        }

        /// <summary>
        /// Updates the provisioning status.
        /// </summary>
        /// <param name="newState">Target new state.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="newTime">Time if that is being set.</param>
        /// <returns>Returns if the update occured.</returns>
        public bool UpdateProvisioningStatus(OperationState newState, string trigger, DateTime? newTime = null)
        {
            if (ProvisioningStatus == newState)
            {
                return false;
            }

            if (ProvisioningStatusChanges == null)
            {
                ProvisioningStatusChanges = new List<OperationStateChanges>();
            }

            var time = newTime.GetValueOrDefault(DateTime.UtcNow);
            ProvisioningStatus = newState;
            ProvisioningStatusChanged = time;
            ProvisioningStatusChanges.Add(new OperationStateChanges
            {
                Status = newState,
                Time = time,
                Trigger = trigger,
            });

            if (newState == OperationState.Succeeded)
            {
                if (Type == ResourceType.StorageFileShare)
                {
                    // Storage resources are ready once they are provisioned. While compute resources are ready when heartbeat is received.
                    IsReady = true;
                    Ready = time;
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the starting status.
        /// </summary>
        /// <param name="newState">Target new state.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="newTime">Time if that is being set.</param>
        /// <returns>Returns if the update occured.</returns>
        public bool UpdateStartingStatus(OperationState newState, string trigger, DateTime? newTime = null)
        {
            if (StartingStatus.HasValue && StartingStatus.Value == newState)
            {
                return false;
            }

            if (StartingStatusChanges == null)
            {
                StartingStatusChanges = new List<OperationStateChanges>();
            }

            var time = newTime.GetValueOrDefault(DateTime.UtcNow);
            StartingStatus = newState;
            StartingStatusChanged = time;
            StartingStatusChanges.Add(new OperationStateChanges
            {
                Status = newState,
                Time = time,
                Trigger = trigger,
            });

            return true;
        }

        /// <summary>
        /// Updates the deleting status.
        /// </summary>
        /// <param name="newState">Target new state.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="newTime">Time if that is being set.</param>
        /// <returns>Returns if the update occured.</returns>
        public bool UpdateDeletingStatus(OperationState newState, string trigger, DateTime? newTime = null)
        {
            if (DeletingStatus.HasValue && DeletingStatus.Value == newState)
            {
                return false;
            }

            if (DeletingStatusChanges == null)
            {
                DeletingStatusChanges = new List<OperationStateChanges>();
            }

            var time = newTime.GetValueOrDefault(DateTime.UtcNow);
            DeletingStatus = newState;
            DeletingStatusChanged = time;
            DeletingStatusChanges.Add(new OperationStateChanges
            {
                Status = newState,
                Time = time,
                Trigger = trigger,
            });

            if (newState == OperationState.Initialized)
            {
                IsReady = false;
                IsDeleted = true;
            }

            return true;
        }

        /// <summary>
        /// Updates the cleanup status.
        /// </summary>
        /// <param name="newState">Target new state.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="newTime">Time if that is being set.</param>
        /// <returns>Returns if the update occured.</returns>
        public bool UpdateCleanupStatus(OperationState newState, string trigger, DateTime? newTime = null)
        {
            if (CleanupStatus.HasValue && CleanupStatus.Value == newState)
            {
                return false;
            }

            if (CleanupStatusChanges == null)
            {
                CleanupStatusChanges = new List<OperationStateChanges>();
            }

            var time = newTime.GetValueOrDefault(DateTime.UtcNow);
            CleanupStatus = newState;
            CleanupStatusChanged = time;
            CleanupStatusChanges.Add(new OperationStateChanges
            {
                Status = newState,
                Time = time,
                Trigger = trigger,
            });

            return true;
        }
    }
}
