// <copyright file="VsoAccountInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    /// <summary>
    /// Static (unchanging) identifying information about an account, included in both
    /// <see cref="BillingAccount" /> and <see cref="BillingEvent" /> entities.
    /// </summary>
    public class VsoAccountInfo : IEquatable<VsoAccountInfo>
    {
        /// <summary>Azure Resource Provider namespace.</summary>
        public const string ProviderName = "Microsoft.VSOnline";

        /// <summary>Resource type of account resources.</summary>
        public const string AccountResourceType = "accounts";

        // String constants used in resource URIs.
        private const string Subscriptions = "subscriptions";
        private const string ResourceGroups = "resourceGroups";
        private const string Providers = "providers";

        /// <summary>
        /// Gets or sets the ID of the subscription that contains the account resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "subscription")]
        public string Subscription { get; set; }

        /// <summary>
        /// Gets or sets the name (not full path) of the resource group that contains the account resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the name (not full path) of the account resource.
        /// </summary>
        /// <remarks>
        /// The full resource path can be obtained via the <see cref="AccountExtensions.GetResourcePath()" /> extension method.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the geo-location (region) that the account resource is in.
        /// </summary>
        /// <remarks>
        /// All environments associated with an account must be in the same region as the account.
        /// <para/>
        /// At least initially there will be a separate database per region, so all entities in
        /// the same database will have the location value. But this property can allow for
        /// multiple regions sharing the same database if that is ever preferable.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets the Azure resource ID (path) of the account, which is in the form
        /// `/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/{ProviderName}/{AccountResourceType}/{Name}`.
        /// </summary>
        public string ResourceId
        {
            get
            {
                Requires.Argument(IsValidSubscriptionId(Subscription), nameof(Subscription), "Invalid subscription ID.");
                Requires.Argument(IsValidResourceGroupName(ResourceGroup), nameof(ResourceGroup), "Invalid resource group name.");
                Requires.Argument(IsValidAccountName(Name), nameof(Name), "Invalid account name.");

                return $"/{Subscriptions}/{Subscription}/{ResourceGroups}/{ResourceGroup}" +
                    $"/{Providers}/{ProviderName}/{AccountResourceType}/{Name}";
            }
        }

        public static bool operator ==(VsoAccountInfo a, VsoAccountInfo b) =>
           (object)a == null ? (object)b == null : a.Equals(b);

        public static bool operator !=(VsoAccountInfo a, VsoAccountInfo b) => !(a == b);

        /// <summary>
        /// Attempts to parse an Azure resource ID into a `VsoAccountInfo` object.
        /// </summary>
        /// <param name="resourceId">String in the same form as the <see cref="ResourceId"/> property.</param>
        /// <param name="account">Resulting account object, or null if parsing failed.</param>
        /// <returns>True if the parse succeeded, otherwise false.</returns>
        /// <remarks>
        /// The resulting account object does not include a location. That may be set separately.
        /// </remarks>
        public static bool TryParse(string resourceId, out VsoAccountInfo account)
        {
            Requires.NotNullOrEmpty(resourceId, nameof(resourceId));

            var parts = resourceId.Split('/');

            if (parts.Length != 9 ||
                parts[0].Length != 0 ||
                parts[1] != Subscriptions ||
                parts[3] != ResourceGroups ||
                parts[5] != Providers ||
                parts[6] != ProviderName ||
                parts[7] != AccountResourceType)
            {
                account = null;
                return false;
            }

            string subscription = parts[2];
            string resourceGroup = parts[4];
            string name = parts[8];

            if (!IsValidSubscriptionId(subscription) ||
                !IsValidResourceGroupName(resourceGroup) ||
                !IsValidAccountName(name))
            {
                account = null;
                return false;
            }

            account = new VsoAccountInfo
            {
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                Name = name,
            };
            return true;
        }

        /// <summary> Tests if this account equals another account.</summary>
        /// <param name="other">Another account object.</param>
        /// <returns>True if all account properties are equal (including location).</returns>
        public bool Equals(VsoAccountInfo other)
        {
            return (object)other != null &&
                this.Subscription == other.Subscription &&
                this.ResourceGroup == other.ResourceGroup &&
                this.Name == other.Name &&
                this.Location == other.Location;
        }

        /// <summary> Tests if this account equals another object.</summary>
        /// <param name="obj">Another object.</param>
        /// <returns>True if the other object is an account and all account properties are equal (including location).</returns>
        public override bool Equals(object obj) => Equals(obj as VsoAccountInfo);

        /// <summary>Gets a hashcode for the account.</summary>
        /// <returns>Hash code derived from the account subscription.</returns>
        public override int GetHashCode() => Subscription?.GetHashCode() ?? 0;

        private static bool IsValidSubscriptionId(string subscriptionId)
        {
            return Guid.TryParseExact(subscriptionId, "D", out _);
        }

        private static bool IsValidResourceGroupName(string resourceGroupName)
        {
            // https://docs.microsoft.com/en-us/azure/architecture/best-practices/naming-conventions#naming-rules-and-restrictions
            return !string.IsNullOrWhiteSpace(resourceGroupName) && resourceGroupName.Length <= 90;
        }

        private static bool IsValidAccountName(string accountName)
        {
            // Prevent a query string (API version) from being inadvertantly concatenated an the account name.
            return !string.IsNullOrWhiteSpace(accountName) && accountName.IndexOf('?') < 0;
        }
    }
}
