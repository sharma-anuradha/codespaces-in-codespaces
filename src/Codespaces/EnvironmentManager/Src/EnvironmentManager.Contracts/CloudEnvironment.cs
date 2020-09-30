// <copyright file="CloudEnvironment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The top-level environment entity.
    /// </summary>
    public class CloudEnvironment : TaggedEntity
    {
        private AzureLocation? controlPlaneLocation = null;
        private string friendlyName;
        private string friendlyNameInLowerCase;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironment"/> class.
        /// </summary>
        public CloudEnvironment()
        {
            Transitions = new CloudEnvironmentTransitions();
            Connection = new ConnectionInfo();
        }

        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public EnvironmentType Type { get; set; }

        /// <summary>
        /// Gets or sets the environment friendly name.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "friendlyName")]
        public string FriendlyName
        {
            get
            {
                return friendlyName;
            }

            set
            {
                friendlyName = value;
                friendlyNameInLowerCase = value?.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets the environment friendly name in lowercase
        /// This property is used to validating the environment name irrespective of case.
        /// This helps for a cost efficient query during lookups.
        /// It always sets to Friendly name value just to be in sync.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "friendlyNameInLowerCase")]
        public string FriendlyNameInLowerCase
        {
            get
            {
                return friendlyNameInLowerCase ?? FriendlyName?.ToLowerInvariant();
            }

            set
            {
                // Intentional no-op.
                // The value must be set via FriendlyName, and not independently so that they will never be out of synch.
                // The no-op setter allows the data entity serializer/deserializer to operate correctly.
            }
        }

        /// <summary>
        /// Gets or sets the created date and time.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the updated date and time.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "updated")]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the environment state.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public CloudEnvironmentState State { get; set; }

        /// <summary>
        /// Gets or sets an optional timeout assigned to transitional states.
        /// </summary>
        /// <remarks>
        /// Used in conjuction with <see cref="State"/> and <see cref="LastStateUpdated"/> to assign a timeout after
        /// which the current transitional state is considered to be invalid and environment needs to be repaired.
        /// </remarks>
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "stateTimeout")]
        public DateTime? StateTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the code space is marked as deleted.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "isDeleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the code space is marked as deleted.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "lastDeleted")]
        public DateTime? LastDeleted { get; set; }

        /// <summary>
        /// Gets or sets the updated date and time for state change.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "lastStateUpdated")]
        public DateTime LastStateUpdated { get; set; }

        /// <summary>
        /// Gets or sets the trigger for the last state change.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "lastUpdateTrigger")]
        public string LastStateUpdateTrigger { get; set; }

        /// <summary>
        /// Gets or sets the reason for state change.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "lastStateUpdateReason")]
        public string LastStateUpdateReason { get; set; }

        /// <summary>
        /// Gets or sets the scheduled archival date, after which the environment will be queued up for archival.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "scheduledArchival")]
        public DateTime? ScheduledArchival { get; set; }

        /// <summary>
        /// Gets or sets the continer image name.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "containerImage")]
        public string ContainerImage { get; set; }

        /// <summary>
        /// Gets or sets the environment seed info.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "seed")]
        public SeedInfo Seed { get; set; }

        /// <summary>
        /// Gets or sets the environment personalization info.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "personalization")]
        public PersonalizationInfo Personalization { get; set; }

        /// <summary>
        /// Gets or sets the environment connection info.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "connection")]
        public ConnectionInfo Connection { get; set; }

        /// <summary>
        /// Gets or sets session paths for folders opened in an environment.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "recentFolders")]
        public List<string> RecentFolders { get; set; }

        /// <summary>
        /// Gets or sets the last active date and time.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "active")]
        public DateTime Active { get; set; }

        /// <summary>
        /// Gets or sets the fully-qualified Azure resource id of the Account object.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "planId")]
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the environment sku name.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "skuName")]
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the cloud environment Azure location.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the cloud environment Azure location.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "controlPlaneLocation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation ControlPlaneLocation
        {
            get { return controlPlaneLocation ?? Location; }
            set { controlPlaneLocation = value; }
        }

        /// <summary>
        /// Gets or sets the environment storage info.
        /// </summary>
        /// <remarks>
        /// Returned by back-end resource broker AllocateResult.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "storage")]
        public ResourceAllocationRecord Storage { get; set; }

        /// <summary>
        /// Gets or sets the environment compute info.
        /// </summary>
        /// <remarks>
        /// Returned by back-end resource broker AllocateResult.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "compute")]
        public ResourceAllocationRecord Compute { get; set; }

        /// <summary>
        /// Gets or sets the environment OS disk.
        /// </summary>
        /// <remarks>
        /// Returned by back-end resource broker AllocateResult.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "osDisk")]
        public ResourceAllocationRecord OSDisk { get; set; }

        /// <summary>
        /// Gets or sets the environment OS disk snapshot.
        /// </summary>
        /// <remarks>
        /// Returned by back-end resource broker AllocateResult.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "osDiskSnapshot")]
        public ResourceAllocationRecord OSDiskSnapshot { get; set; }

        /// <summary>
        /// Gets or sets the exported environment blob storage url.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "exportedBlobUrl")]
        public string ExportedBlobUrl { get; set; }

        /// <summary>
        /// The branch where changes were exported.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "exportedBranch")]
        public string ExportedBranch { get; set; }

        /// <summary>
        /// Gets or sets the heartbeat record id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "heartbeatResourceId")]
        public string HeartbeatResourceId { get; set; }

        /// <summary>
        /// Gets or sets the last time the record is updated based on heartbeat.
        /// </summary>
        [Obsolete("Replaced with heartbeat record.")]
        public DateTime LastUpdatedByHeartBeat { get; set; }

        /// <summary>
        /// Gets or sets the last time the record is updated based on active sessions on environment.
        /// </summary>
        [JsonIgnore]
        public DateTime LastUsed
        {
            // Its computed as follows:
            // 1. if SessionStart and SessionEnd is default, return Created
            // 2. If SessionStart is set, retun current time
            // 3. If SessionStart is default and SessionEnd is Set return SessionEnd
            get
            {
                if (SessionEnded != default)
                {
                    // There is no active session.
                    return SessionEnded.Value;
                }

                if (SessionEnded == default && SessionStarted != default)
                {
                    // There is an active session.
                    return DateTime.UtcNow;
                }

                return Created;
            }
        }

        /// <summary>
        /// Gets or sets the time environment state changed to avaialble state.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "sessionStarted")]
        public DateTime? SessionStarted { get; set; }

        /// <summary>
        /// Gets or sets the time environment state changed from avaialble state.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "sessionEnded")]
        public DateTime? SessionEnded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment has unpushed git changes.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "hasUnpushedGitChanges")]
        public bool? HasUnpushedGitChanges { get; set; }

        /// <summary>
        /// Gets or sets the auto shutdown time the user specified.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "autoShutdownDelayMinutes")]
        public int AutoShutdownDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets the feature flags.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "features")]
        public Dictionary<string, string> Features { get; set; }

        /// <summary>
        /// Gets or sets the transitions.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "transitions")]
        public CloudEnvironmentTransitions Transitions { get; set; }

        /// <summary>
        /// Gets or sets the partner. See <see cref="VsoPlan.Partner"/>.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "partner")]
        public Partner? Partner { get; set; }

        /// <summary>
        /// Gets or sets the environment metrics values at creation time.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "creationMetrics")]
        public MetricsInfo CreationMetrics { get; set; }

        /// <summary>
        /// Gets or sets the subnet for codespace.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "subnetResourceId")]
        public string SubnetResourceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cloud-environment record exists in the regional database.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "isMigrated")]
        public bool IsMigrated { get; set; }

        /// <summary>
        /// Gets or sets the Subscription Data.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "subscriptionData")]
        public SubscriptionData SubscriptionData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment allows to queue resource allocation request.
        /// </summary>
        [JsonProperty(PropertyName = "queueResourceAllocation")]
        public bool QueueResourceAllocation { get; set; }

        /// <summary>
        /// Indicates whether the environment is in a shutdown state.
        /// </summary>
        /// <returns>Whether it is shutdown.</returns>
        public bool IsShutdownOrArchived()
        {
            return State == CloudEnvironmentState.Shutdown
                || State == CloudEnvironmentState.Archived;
        }
    }
}