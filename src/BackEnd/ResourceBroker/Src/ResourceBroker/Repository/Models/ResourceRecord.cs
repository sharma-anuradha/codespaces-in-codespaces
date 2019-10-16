// <copyright file="ResourceRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource record.
    /// </summary>
    public class ResourceRecord : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the resource sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the azure resource info from the compute or storage provider.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the azure location.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is ready.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Gets or sets the ready date.
        /// </summary>
        public DateTime? Ready { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is assigned.
        /// </summary>
        public bool IsAssigned { get; set; }

        /// <summary>
        /// Gets or sets the assigned date.
        /// </summary>
        public DateTime? Assigned { get; set; }

        /// <summary>
        /// Gets or sets the created date.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the pool reference details.
        /// </summary>
        public ResourcePoolDefinitionRecord PoolReference { get; set; }

        /// <summary>
        /// Gets or sets the provisioning reason.
        /// </summary>
        public string ProvisioningReason { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? ProvisioningStatus { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status Changed date.
        /// </summary>
        public DateTime? ProvisioningStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Provisioning Status Changes.
        /// </summary>
        public IList<OperationStateChanges> ProvisioningStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the starting reason.
        /// </summary>
        public string StartingReason { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? StartingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status Changed date.
        /// </summary>
        public DateTime? StartingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Starting Status Changes.
        /// </summary>
        public IList<OperationStateChanges> StartingStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the count of how many times we have tried to delete the resource.
        /// </summary>
        public int DeleteAttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the starting reason.
        /// </summary>
        public string DeletingReason { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? DeletingStatus { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status Changed date.
        /// </summary>
        public DateTime? DeletingStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the Deleting Status Changes.
        /// </summary>
        public IList<OperationStateChanges> DeletingStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the current state of the Keep Alives.
        /// </summary>
        public ResourceKeepAliveRecord KeepAlives { get; set; }

        /// <summary>
        /// Gets or sets the cleanup reason.
        /// </summary>
        public string CleanupReason { get; set; }

        /// <summary>
        /// Gets or sets the cleanup status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? CleanupStatus { get; set; }

        /// <summary>
        /// Gets or sets the cleanup Status Changed date.
        /// </summary>
        public DateTime? CleanupStatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the cleanup Status Changes.
        /// </summary>
        public IList<OperationStateChanges> CleanupStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the current HeartBeat.
        /// </summary>
        public ResourceHeartBeatSummaryRecord HeartBeatSummary { get; set; }

        /// <summary>
        /// Gets or sets the Properties.
        /// </summary>
        public dynamic Properties { get; set; }

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
                IsReady = true;
                Ready = time;
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
