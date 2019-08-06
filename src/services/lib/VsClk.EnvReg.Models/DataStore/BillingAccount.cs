using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    /// <summary>
    /// Database entity that represents an account
    /// </summary>
    public class BillingAccount : TaggedEntity
    {
        [JsonProperty(Required = Required.Always, PropertyName = "account")]
        public BillingAccountInfo Account { get; set; }

        /// <summary>
        /// The Cloud Environments (VSLS) profile ID of the user who created the account.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// The billing plan selected for this account. This corresponds to the "SKU"
        /// property on the account resource.
        /// </summary>
        /// <remarks>
        /// Changes to this property should also be recorded as
        /// <see cref="BillingEventTypes.AccountPlanChange"/> events in the billing events collection.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "plan")]
        public string Plan { get; set; }

        /// <summary>
        /// Current state of the subscription, which may impact what operations and billing are allowed.
        /// </summary>
        /// <seealso cref="SubscriptionStates" />
        /// <remarks>
        /// Changes to this property should also be recorded as
        /// <see cref="BillingEventTypes.SubscriptionStateChange"/> events in the billing events collection.
        /// <para/>
        /// While there may be multiple accounts in a subscription so subscription state is not really
        /// per-account, it is tracked separately with each account because billing is account-centric.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "subscriptionState")]
        public string SubscriptionState { get; set; }
    }

    /// <summary>
    /// Static (unchanging) identifying information about an account, included in both
    /// <see cref="BillingAccount" /> and <see cref="BillingEvent" /> entities.
    /// </summary>
    public class BillingAccountInfo
    {
        /// <summary>
        /// ID of the subscription that contains the account resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "subscription")]
        public string Subscription { get; set; }

        /// <summary>
        /// Name (not full path) of the resource group that contains the account resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Name (not full path) of the account resource.
        /// </summary>
        /// <remarks>
        /// The full resource path can be obtained via the <see cref="AccountExtensions.GetResourcePath()" /> extension method.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Geo-location (region) that the account resource is in.
        /// </summary>
        /// <remarks>
        /// All environments associated with an account must be in the same region as the account.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        public string Location { get; set; }
    }
}
