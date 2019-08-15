using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    /// <summary>
    /// Records a summary of billable activity during one billing "period".
    /// </summary>
    /// <remarks>
    /// A billing period typically covers one hour, but in some cases it may be for
    /// a partial hour (if the subscription state or account plan changed) or
    /// multiple hours (when contiguous hours of zero activity are combined).
    /// </remarks>
    public class BillingSummary
    {
        /// <summary>
        /// UTC start of the period covered by this summary.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "periodStart")]
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// UTC end of the period covered by this summary.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "periodEnd")]
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Account plan used for this billing period.
        /// </summary>
        /// <remarks>
        /// If the account plan changes during the middle of the default billing period (hour), the
        /// period should be split at that point.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "plan")]
        public string Plan { get; set; }

        /// <summary>
        /// State of the subscription during this billing period.
        /// </summary>
        /// <seealso cref="SubscriptionStates" />
        /// <remarks>
        /// If the subscription state changes during the middle of the default billing period (hour),
        /// the period should be split at that point.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "subscriptionState")]
        public string SubscriptionState { get; set; }

        /// <summary>
        /// Total usage billed to the account for the billing period for one or more
        /// billing meters, in each meter's units.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public IDictionary<string, double> Usage { get; set; }

        /// <summary>
        /// A more detailed breakdown of usage for the period. Supplemental information
        /// that does not contribute directly to the billed usage.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usageDetail")]
        public UsageDetail UsageDetail { get; set; }

        /// <summary>
        /// True if this billing data was emitted so that the customer will actually get
        /// billed for this usage; false if billing for this subscription/account/period was
        /// disabled or there was no usage during this period.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "emitted")]
        public bool Emitted { get; set; }
    }

    public class UsageDetail
    {
        /// <summary>
        /// Mapping from environment IDs to environment usage details.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "environments")]
        public IDictionary<string, EnvironmentUsageDetail> Environments { get; set; }

        /// <summary>
        /// Mapping from user IDs to user usage details.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "users")]
        public IDictionary<string, UserUsageDetail> Users { get; set; }
    }

    public class EnvironmentUsageDetail
    {
        /// <summary>
        /// Environment name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        // TODO: Consider adding other environment details here that might
        // be relevant to understanding the bill, such as the env size.

        /// <summary>
        /// State of the environment at the end of the period.
        /// </summary>
        /// <seealso cref="EnvironmentStates" />
        /// <remarks>
        /// This can be used by the billing service to efficiently find the current state of
        /// active environments (those which have current or recent usage), by starting with the
        /// most recent billing summary event and applying any state change events after that.
        /// Or if there was no usage detail for an environment for the previous period summary
        /// then the environment must have been inactive during that entire period.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "endState")]
        public string EndState { get; set; }

        /// <summary>
        /// Usage billed to the environment for the billing period for one or more
        /// billing meters, in each meter's units.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public IDictionary<string, double> Usage { get; set; }
    }

    public class UserUsageDetail
    {
        // TODO: Consider adding a user name or email property here
        // to avoid having to do lots of lookups of the user ID.

        /// <summary>
        /// Total usage billed to the user for the billing period for one or more
        /// billing meters, in each meter's units.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public IDictionary<string, double> Usage { get; set; }
    }
}
