// <copyright file="VsoAccountInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    /// <summary>
    /// Static (unchanging) identifying information about an account, included in both
    /// <see cref="BillingAccount" /> and <see cref="BillingEvent" /> entities.
    /// </summary>
    public class VsoAccountInfo : IEquatable<VsoAccountInfo>
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

        public static bool operator ==(VsoAccountInfo a, VsoAccountInfo b) =>
           (object)a == null ? (object)b == null : a.Equals(b);

        public static bool operator !=(VsoAccountInfo a, VsoAccountInfo b) => !(a == b);

        public bool Equals(VsoAccountInfo other)
        {
            return (object)other != null &&
                this.Subscription == other.Subscription &&
                this.ResourceGroup == other.ResourceGroup &&
                this.Name == other.Name &&
                this.Location == other.Location;
        }

        public override bool Equals(object obj) => Equals(obj as VsoAccountInfo);

        public override int GetHashCode() => Subscription?.GetHashCode() ?? 0;
    }
}
