// <copyright file="VsoPlanInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Static (unchanging) identifying information about an plan, included in both
    /// <see cref="BillingAccount" /> and <see cref="BillingEvent" /> entities.
    /// </summary>
    public class VsoPlanInfo : IEquatable<VsoPlanInfo>
    {
        /// <summary>Azure Resource Provider namespace.</summary>
        public const string ProviderName = "Microsoft.VSOnline";

        /// <summary>Resource type of plan resources.</summary>
        public const string PlanResourceType = "plans";

        // String constants used in resource URIs.
        private const string Subscriptions = "subscriptions";
        private const string ResourceGroups = "resourceGroups";
        private const string Providers = "providers";

        /// <summary>
        /// Gets or sets the ID of the subscription that contains the plan resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "subscription")]
        public string Subscription { get; set; }

        /// <summary>
        /// Gets or sets the name (not full path) of the resource group that contains the plan resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the name (not full path) of the plan resource.
        /// </summary>
        /// <remarks>
        /// The full resource path can be obtained via the <see cref="AccountExtensions.GetResourcePath()" /> extension method.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the geo-location (region) that the plan resource is in.
        /// </summary>
        /// <remarks>
        /// All environments associated with an plan must be in the same region as the plan.
        /// <para/>
        /// At least initially there will be a separate database per region, so all entities in
        /// the same database will have the location value. But this property can allow for
        /// multiple regions sharing the same database if that is ever preferable.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets the Azure resource ID (path) of the plan, which is in the form
        /// `/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/{ProviderName}/{PlanResourceType}/{Name}`.
        /// </summary>
        [JsonIgnore]
        public string ResourceId
        {
            get
            {
                Requires.Argument(IsValidSubscriptionId(Subscription), nameof(Subscription), "Invalid subscription ID.");
                Requires.Argument(IsValidResourceGroupName(ResourceGroup), nameof(ResourceGroup), "Invalid resource group name.");
                Requires.Argument(IsValidPlanName(Name), nameof(Name), "Invalid plan name.");

                return $"/{Subscriptions}/{Subscription}/{ResourceGroups}/{ResourceGroup}" +
                    $"/{Providers}/{ProviderName}/{PlanResourceType}/{Name}";
            }
        }

        public static bool operator ==(VsoPlanInfo a, VsoPlanInfo b) =>
           a is null ? b is null : a.Equals(b);

        public static bool operator !=(VsoPlanInfo a, VsoPlanInfo b) => !(a == b);

        /// <summary>
        /// Attempts to parse an Azure resource ID into a `VsoPlanInfo` object.
        /// </summary>
        /// <param name="resourceId">String in the same form as the <see cref="ResourceId"/> property.</param>
        /// <param name="plan">Resulting plan object, or null if parsing failed.</param>
        /// <returns>True if the parse succeeded, otherwise false.</returns>
        /// <remarks>
        /// The resulting plan object does not include a location. That may be set separately.
        /// </remarks>
        public static bool TryParse(string resourceId, out VsoPlanInfo plan)
        {
            Requires.NotNullOrEmpty(resourceId, nameof(resourceId));

            var parts = resourceId.Split('/');

            if (parts.Length != 9 ||
                parts[0].Length != 0 ||
                parts[1] != Subscriptions ||
                parts[3] != ResourceGroups ||
                parts[5] != Providers ||
                parts[6] != ProviderName ||
                parts[7] != PlanResourceType)
            {
                plan = null;
                return false;
            }

            var subscription = parts[2];
            var resourceGroup = parts[4];
            var name = parts[8];

            if (!IsValidSubscriptionId(subscription) ||
                !IsValidResourceGroupName(resourceGroup) ||
                !IsValidPlanName(name))
            {
                plan = null;
                return false;
            }

            plan = new VsoPlanInfo
            {
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                Name = name,
            };
            return true;
        }

        /// <summary>
        /// Attempts to parse an Azure resource ID into a `VsoPlanInfo` object.
        /// </summary>
        /// <param name="resourceId">String in the same form as the <see cref="ResourceId"/> property.</param>
        /// <returns>VsoPlanInfo object.</returns>
        /// <remarks>
        /// The resulting plan object does not include a location. That may be set separately.
        /// </remarks>
        public static VsoPlanInfo TryParse(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return null;
            }

            var parts = resourceId.Split('/');
            var plan = new VsoPlanInfo();

            if (parts.Length != 9 ||
                parts[0].Length != 0 ||
                parts[1] != Subscriptions ||
                parts[3] != ResourceGroups ||
                parts[5] != Providers ||
                parts[6] != ProviderName ||
                parts[7] != PlanResourceType)
            {
                plan = null;
            }

            var subscription = parts[2];
            var resourceGroup = parts[4];
            var name = parts[8];

            if (!IsValidSubscriptionId(subscription) ||
                !IsValidResourceGroupName(resourceGroup) ||
                !IsValidPlanName(name))
            {
                plan = null;
            }

            plan.Subscription = subscription;
            plan.ResourceGroup = resourceGroup;
            plan.Name = name;
            return plan;
        }

        /// <summary> Tests if this plan equals another plan.</summary>
        /// <param name="other">Another plan object.</param>
        /// <returns>True if all plan properties are equal (including location).</returns>
        public bool Equals(VsoPlanInfo other)
        {
            return other != null &&
                Subscription == other.Subscription &&
                ResourceGroup == other.ResourceGroup &&
                Name == other.Name &&
                Location == other.Location;
        }

        /// <summary> Tests if this plan equals another object.</summary>
        /// <param name="obj">Another object.</param>
        /// <returns>True if the other object is an plan and all plan properties are equal (including location).</returns>
        public override bool Equals(object obj) => Equals(obj as VsoPlanInfo);

        /// <summary>Gets a hashcode for the plan.</summary>
        /// <returns>Hash code derived from the plan subscription.</returns>
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

        private static bool IsValidPlanName(string planName)
        {
            // Prevent a query string (API version) from being inadvertantly concatenated an the plan name.
            return !string.IsNullOrWhiteSpace(planName) && planName.IndexOf('?') < 0;
        }
    }
}
