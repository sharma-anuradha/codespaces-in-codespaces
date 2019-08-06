using System;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    /// <summary>
    /// Database entity that represents an event that is important to track either
    /// directly for billing or for billing-related monitoring & consistency checking.
    /// </summary>
    public class BillingEvent : TaggedEntity
    {
        /// <summary>
        /// UTC time when the event occurred.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "time")]
        public DateTime Time { get; set; }

        /// <summary>
        /// Information about the account that the event relates to.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "account")]
        public BillingAccountInfo Account { get; set; }

        /// <summary>
        /// Optional environment info. Required for some event types, but may be omitted
        /// if the event type is not associated with a specific environment.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "environment")]
        public EnvironmentInfo Environment { get; set; }

        /// <summary>
        /// One of the <see cref="BillingEventTypes" /> constants.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// The type of object depends on the <see cref="EventType" />.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "args")]
        public object Args { get; set; }
    }

    public static class BillingEventTypes
    {
        /// <summary>
        /// Event that occurs when we are notified by our RP that a subscription state changed.
        /// An initial event should also be emitted when each account is created.
        /// </summary>
        /// <seealso cref="SubscriptionStates" />
        /// <seealso cref="BillingStateChange" />
        public const string SubscriptionStateChange = "subscriptionStateChange";

        /// <summary>
        /// Event that occurrs when we are notified by our RP that an account plan (SKU)
        /// changed. An initial event should also be emitted when each account is created.
        /// </summary>
        /// <seealso cref="BillingStateChange" />
        public const string AccountPlanChange = "accountPlanChange";

        /// <summary>
        /// Event that occurrs when a cloud environment state changes.
        /// </summary>
        /// <seealso cref="EnvironmentStates" />
        /// <seealso cref="BillingStateChange" />
        public const string EnvironmentStateChange = "environmentStateChange";

        /// <summary>
        /// Event that occurs when one billing for one period has been calculated and possibly emitted.
        /// </summary>
        /// <seealso cref="BillingSummary" />
        public const string BillingSummary = "billingSummary";
    }

    public class BillingStateChange
    {
        [JsonProperty(Required = Required.Always, PropertyName = "oldValue")]
        public string OldValue { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "newValue")]
        public string NewValue { get; set; }
    }

    /// <summary>
    /// See https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/subscription-lifecycle-api-reference.md#subscription-states
    /// </summary>
    public static class SubscriptionStates
    {
        /// <summary>Initial and terminal state. </summary>
        public const string Unregistered = "unregistered";

        public const string Registered = "registered";
        public const string Warned = "warned";
        public const string Suspended = "suspended";
        public const string Deleted = "deleted";
    }

    public static class EnvironmentStates
    {
        // TODO: Update states based on actual state transitions driven by environment management service.

        /// <summary>Initial state.</summary>
        public const string Created = "created";

        public const string Provisioning = "provisioning";
        public const string Resuming = "resuming";
        public const string Running = "running";
        public const string Connected = "connected";
        public const string Suspending = "suspending";
        public const string Suspended = "suspended";

        /// <summary>Terminal state.</summary>
        public const string Deleted = "deleted";
    }

    public class EnvironmentInfo
    {
        /// <summary>
        /// Environment ID.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// User-assigned name of the environment.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The Cloud Environments (VSLS) profile ID of the user of the environment
        /// (not necessarily the account owner).
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "userId")]
        public string UserId { get; set; }
    }
}
