// <copyright file="VsoVnetProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Plan properties for VNet Injection.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class VsoVnetProperties
    {
        /// <summary>
        /// Gets or sets the subnet resource id to use for creating environments in this plan.
        /// </summary>
        public string SubnetId { get; set; }

        public static bool operator ==(VsoVnetProperties a, VsoVnetProperties b) =>
           a is null ? b is null : a.Equals(b);

        public static bool operator !=(VsoVnetProperties a, VsoVnetProperties b) => !(a == b);

        /// <summary> Tests if this plan settings equals another plan settings.</summary>
        /// <param name="other">Another plan settings object.</param>
        /// <returns>True if all plan properties are equal.</returns>
        public bool Equals(VsoVnetProperties other)
        {
            return other != default &&
              (SubnetId == other.SubnetId ||
                (SubnetId != default &&
                SubnetId.Equals(other.SubnetId, System.StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary> Tests if this plan settings equals another plan settings.</summary>
        /// <param name="obj">Another plan settings object.</param>
        /// <returns>True if all plan properties are equal.</returns>
        public override bool Equals(object obj) => Equals(obj as VsoVnetProperties);

        /// <summary>Gets a hashcode for the plan settings.</summary>
        /// <returns>Hash code derived from each of the plan settings properties.</returns>
        public override int GetHashCode() => SubnetId?.GetHashCode() ?? 0;
    }
}
