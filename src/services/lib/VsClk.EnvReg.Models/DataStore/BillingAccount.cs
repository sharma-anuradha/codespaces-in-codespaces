using System;
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
    public class BillingAccountInfo : IEquatable<BillingAccountInfo>
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
        /// <para/>
        /// At least initially there will be a separate database per region, so all entities in
        /// the same database will have the location value. But this property can allow for
        /// multiple regions sharing the same database if that is ever preferable.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        public string Location { get; set; }

        public bool Equals(BillingAccountInfo other)
        {
            return (object)other != null &&
                this.Subscription == other.Subscription &&
                this.ResourceGroup == other.ResourceGroup &&
                this.Name == other.Name &&
                this.Location == other.Location;
        }

        public override bool Equals(object obj) => Equals(obj as BillingAccountInfo);

        public override int GetHashCode() => HashCode.Combine(Subscription, Name);

        public static bool operator ==(BillingAccountInfo a, BillingAccountInfo b) =>
           (object)a == null ? (object)b == null : a.Equals(b);
        public static bool operator !=(BillingAccountInfo a, BillingAccountInfo b) => !(a == b);
    }
}
