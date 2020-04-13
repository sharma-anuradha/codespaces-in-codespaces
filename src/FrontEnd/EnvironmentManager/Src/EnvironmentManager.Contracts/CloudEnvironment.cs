// <copyright file="CloudEnvironment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
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
            Transitions = new CloudEnvironmentTansitions();
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
        public ResourceAllocation Storage { get; set; }

        /// <summary>
        /// Gets or sets the environment compute info.
        /// </summary>
        /// <remarks>
        /// Returned by back-end resource broker AllocateResult.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "compute")]
        public ResourceAllocation Compute { get; set; }

        /// <summary>
        /// Gets or sets the last time the record is updated based on heartbeat.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "lastUpdatedByHeartBeat")]
        public DateTime LastUpdatedByHeartBeat { get; set; }

        /// <summary>
        /// Gets or sets the last time the record is updated based on active sessions on environment.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "lastUsed")]
        public DateTime LastUsed { get; set; }

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
        public CloudEnvironmentTansitions Transitions { get; set; }

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
        /// Indicates whether the environment is in a shutdown state.
        /// </summary>
        /// <returns>Whether it is shutdown.</returns>
        public bool IsShutdown()
        {
            return State == CloudEnvironmentState.Shutdown
                || State == CloudEnvironmentState.Archived;
        }
    }
}