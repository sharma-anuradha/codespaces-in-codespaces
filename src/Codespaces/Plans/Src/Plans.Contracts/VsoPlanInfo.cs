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
        /// <summary>Previous Azure Resource Provider namespace.</summary>
        public const string VsoProviderNamespace = "Microsoft.VSOnline";

        /// <summary>Azure Resource Provider namespace.</summary>
        public const string CodespacesProviderNamespace = "Microsoft.Codespaces";

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
        /// Gets or sets the RP namespace the plan resource was created in.
        /// </summary>
        /// <remarks>
        /// Defaults to "Microsoft.VSOnline" because plans created in that namespace
        /// did not persist the namespace.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "providerNamespace")]
        public string ProviderNamespace { get; set; } = VsoProviderNamespace;

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
        /// All environments associated with an plan must be in the same region as per the plan.
        /// <para/>
        /// At least initially there will be a separate database per region, so all entities in
        /// the same database will have the location value. But this property can allow for
        /// multiple regions sharing the same database if that is ever preferable.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets the Azure resource ID (path) of the plan, which is in the format
        /// `/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/{ProviderName}/{PlanResourceType}/{Name}`.
        /// </summary>
        [JsonIgnore]
        public string ResourceId
        {
            get
            {
                var providerNamespace = ProviderNamespace;
                Requires.Argument(Subscription.IsValidSubscriptionId(), nameof(Subscription), "Invalid subscription ID.");
                Requires.Argument(ResourceGroup.IsValidResourceGroupName(), nameof(ResourceGroup), "Invalid resource group name.");
                Requires.Argument(TryParseProviderNamespace(ref providerNamespace), nameof(ProviderNamespace), "Invalid provider namespace.");
                Requires.Argument(IsValidPlanName(Name), nameof(Name), "Invalid plan name.");

                return $"/{Subscriptions}/{Subscription}/{ResourceGroups}/{ResourceGroup}" +
                    $"/{Providers}/{providerNamespace}/{PlanResourceType}/{Name}";
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
                parts[7] != PlanResourceType)
            {
                plan = null;
                return false;
            }

            var subscription = parts[2];
            var resourceGroup = parts[4];
            var providerNamespace = parts[6];
            var name = parts[8];

            if (!subscription.IsValidSubscriptionId() ||
                !resourceGroup.IsValidResourceGroupName() ||
                !IsValidPlanName(name))
            {
                plan = null;
                return false;
            }

            if (!TryParseProviderNamespace(ref providerNamespace))
            {
                plan = null;
                return false;
            }

            plan = new VsoPlanInfo
            {
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                ProviderNamespace = providerNamespace,
                Name = name,
            };
            return true;
        }

        /// <summary>
        /// Attempts to parse an Azure resource ID into a `VsoPlanInfo` object.
        /// </summary>
        /// <param name="resourceId">String in the same form as the <see cref="ResourceId"/> property.</param>
        /// <returns>VsoPlanInfo object, or null if parsing failed.</returns>
        /// <remarks>
        /// The resulting plan object does not include a location. That may be set separately.
        /// </remarks>
        public static VsoPlanInfo TryParse(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return null;
            }

            if (!TryParse(resourceId, out VsoPlanInfo plan))
            {
                return null;
            }

            return plan;
        }

        /// <summary>
        /// Determines if the input is a valid resource provider, and normalizes the case.
        /// </summary>
        /// <param name="providerNamespace">The resource provider namespace. If valid, the reference
        /// is updated to the case-normalized provider namespace.</param>
        /// <returns>True if valid, else false.</returns>
        public static bool TryParseProviderNamespace(ref string providerNamespace)
        {
            if (providerNamespace == null)
            {
                return false;
            }
            else if (providerNamespace.Equals(VsoPlanInfo.CodespacesProviderNamespace, StringComparison.InvariantCultureIgnoreCase))
            {
                providerNamespace = VsoPlanInfo.CodespacesProviderNamespace;
                return true;
            }
            else if (providerNamespace.Equals(VsoPlanInfo.VsoProviderNamespace, StringComparison.InvariantCultureIgnoreCase))
            {
                providerNamespace = VsoPlanInfo.VsoProviderNamespace;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary> Tests if this plan equals another plan.</summary>
        /// <param name="other">Another plan object.</param>
        /// <returns>True if all plan properties are equal (including location).</returns>
        public bool Equals(VsoPlanInfo other)
        {
            return other != null &&
                Subscription == other.Subscription &&
                ResourceGroup == other.ResourceGroup &&
                ProviderNamespace == other.ProviderNamespace &&
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

        private static bool IsValidPlanName(string planName)
        {
            // Prevent a query string (API version) from being inadvertantly concatenated the plan name.
            return !string.IsNullOrWhiteSpace(planName) && planName.IndexOf('?') < 0;
        }
    }
}
