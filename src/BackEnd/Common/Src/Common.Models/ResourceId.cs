// <copyright file="ResourceId.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Text.RegularExpressions;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// A Cloud Environment back-end resource id.
    /// </summary>
    public struct ResourceId : IEquatable<ResourceId>
    {
        /// <summary>
        /// The empty/blank/default resource id.
        /// </summary>
        public static readonly ResourceId Empty = default;

        /// <summary>
        /// The resource id token string, which is valid for serialization and storage. Is null for empty resource ids.
        /// </summary>
        private readonly string idToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceId"/> struct.
        /// </summary>
        /// <param name="resourceType">The cloud environment resource type.</param>
        /// <param name="instanceId">The resource name.</param>
        /// <param name="subscriptionId">The azure subscription id.</param>
        /// <param name="resourceGroup">The azure resource group.</param>
        /// <param name="location">The azure location.</param>
        public ResourceId(
            ResourceType resourceType,
            Guid instanceId,
            Guid subscriptionId,
            string resourceGroup,
            AzureLocation location)
            : this()
        {
            // Construct an empty id.
            if (resourceType == default &&
                instanceId == default &&
                subscriptionId == default &&
                string.IsNullOrEmpty(resourceGroup) &&
                location == default)
            {
                SubscriptionId = default;
                ResourceGroup = default;
                ResourceType = default;
                InstanceId = default;
                Location = default;
                idToken = null;
            }
            else
            {
                Requires.NotEmpty(subscriptionId, nameof(subscriptionId));
                Requires.NotEmpty(instanceId, nameof(instanceId));
                Requires.NotNullOrEmpty(resourceGroup, nameof(resourceGroup));

                ResourceType = RequiresEnumIsDefined(resourceType, nameof(resourceType));
                InstanceId = instanceId;
                SubscriptionId = subscriptionId;
                ResourceGroup = resourceGroup;
                Location = RequiresEnumIsDefined(location, nameof(location));
                idToken = Parser.FormatId(ResourceType, InstanceId, SubscriptionId, ResourceGroup, Location);
            }
        }

        /// <summary>
        /// Gets the Cloud Environment resource type.
        /// </summary>
        public ResourceType ResourceType { get; }

        /// <summary>
        /// Gets the resource instance id.
        /// </summary>
        public Guid InstanceId { get; }

        /// <summary>
        /// Gets the azure subscription in which this resource has been created.
        /// </summary>
        public Guid SubscriptionId { get; }

        /// <summary>
        /// Gets the Azure resource location.
        /// </summary>
        public AzureLocation Location { get; }

        /// <summary>
        /// Gets the Azure resource group name.
        /// </summary>
        public string ResourceGroup { get; }

        /// <summary>
        /// Returns the resource id token in the format "vssaas/resourcetypes/{resourceType}/instances/{instanceId}/subscriptions/{subscriptionId}/locations/{location}".
        /// </summary>
        /// <param name="id">The resource id.</param>
        public static implicit operator string(ResourceId id) => id.ToString();

        public static bool operator ==(ResourceId first, ResourceId second) => first.Equals(second);

        public static bool operator !=(ResourceId first, ResourceId second) => !first.Equals(second);

        /// <summary>
        /// Parse a Cloud Environment resource token id into a <see cref="ResourceId"/>.
        /// The expected token format is "vssaas/resourcetypes/{resourceType}/instances/{instanceId}/subscriptions/{subscriptionId}/resourcegroups/{resourcegroup}/locations/{location}"
        /// where resourceType is <see cref="ResourceType"/>
        /// where instanceId is <see cref="Guid"/>
        /// where subscriptionId is <see cref="Guid"/>
        /// where resourceGroups is <see cref="string"/>
        /// where location is <see cref="AzureLocation"/>.
        /// </summary>
        /// <param name="idToken">The resource id token string.</param>
        /// <param name="value">The output value.</param>
        /// <returns>True if the value was valid, otherwise false.</returns>
        public static bool TryParse(string idToken, out ResourceId value) => Parser.TryParse(idToken, out value, out _);

        /// <summary>
        /// Parse a Cloud Environment resource id token into a <see cref="ResourceId"/>.
        /// The expected format is "vssaas/resourcetypes/{resourceType}/instances/{instanceId}/subscriptions/{subscriptionId}/resourcegroups/{resourcegroup}/locations/{location}"
        /// where resourceType is <see cref="ResourceType"/>
        /// where instanceId is <see cref="Guid"/>
        /// where subscriptionId is <see cref="Guid"/>
        /// where resourcegroup is <see cref="string"/>
        /// where location is <see cref="AzureLocation"/>.
        /// </summary>
        /// <param name="idToken">The resource id token string.</param>
        /// <returns>The resource id structure.</returns>
        public static ResourceId Parse(string idToken)
        {
            // Note: null and empty are valid inputs, resulting in ResourceId.Empty.
            if (!Parser.TryParse(idToken, out var value, out var reason))
            {
                throw new FormatException(reason);
            }

            return value;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Returns resource id token in the format "vssaas/resourcetypes/{resourceType}/instances/{instanceId}/subscriptions/{subscriptionId}/resourcegroups/{resourcegroup}/locations/{location}".
        /// </summary>
        /// <remarks>
        /// <see cref="object.ToString"/> should not return null.
        /// </remarks>
        public override string ToString() => idToken ?? string.Empty;

        /// <inheritdoc/>
        public bool Equals(ResourceId other) => string.Equals(idToken, other.idToken, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is ResourceId resourceId)
            {
                return Equals(resourceId);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => (idToken?.GetHashCode()).GetValueOrDefault();

        private static T RequiresEnumIsDefined<T>(T value, string paramName)
            where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentException($"Invalid {typeof(T).Name} value: {value}", paramName);
            }

            return value;
        }

        private static class Parser
        {
            private const string Prefix = "vssaas";
            private const string ResourceTypes = "resourcetypes";
            private const string ResourceTypeGroupName = "resourcetype";
            private const string Instances = "instances";
            private const string InstanceIdGroupName = "instanceid";
            private const string Subscriptions = "subscriptions";
            private const string SubscriptionIdGroupName = "subscriptionid";
            private const string ResourceGroups = "resourcegroups";
            private const string ResourceGroupGroupName = "resourcegroup";
            private const string Locations = "locations";
            private const string LocationGroupName = "location";
            private const string AlphaNumericAndHyphen = "0-9a-zA-Z-";
            private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
            private static readonly string IdFormat = $"{Prefix}/{ResourceTypes}/{{0}}/{Instances}/{{1}}/{Subscriptions}/{{2}}/{ResourceGroups}/{{3}}/{Locations}/{{4}}";
            private static readonly string ResourceTypeGroup = $"(?<{ResourceTypeGroupName}>[{AlphaNumericAndHyphen}]+)";
            private static readonly string InstanceIdGroup = $"(?<{InstanceIdGroupName}>[{AlphaNumericAndHyphen}]+)";
            private static readonly string SubscriptionIdGroup = $"(?<{SubscriptionIdGroupName}>[{AlphaNumericAndHyphen}]+)";
            private static readonly string ResourceGroupNameGroup = $"(?<{ResourceGroupGroupName}>[{AlphaNumericAndHyphen}]+)";
            private static readonly string LocationGroup = $"(?<{LocationGroupName}>[{AlphaNumericAndHyphen}]+)";
            private static readonly string ResourceIdExpression = $"^{Prefix}/{ResourceTypes}/{ResourceTypeGroup}/{Instances}/{InstanceIdGroup}/{Subscriptions}/{SubscriptionIdGroup}/{ResourceGroups}/{ResourceGroupNameGroup}/{Locations}/{LocationGroup}$";
            private static readonly Regex ResourceIdRegEx = new Regex(ResourceIdExpression, Options);

            public static string FormatId(ResourceType resourceType, Guid instanceId, Guid subscriptionId, string resourceGroup, AzureLocation location)
            {
                return string.Format(
                    IdFormat,
                    resourceType.ToString(),
                    instanceId,
                    subscriptionId,
                    resourceGroup,
                    location.ToString()).ToLowerInvariant();
            }

            public static bool TryParse(string idToken, out ResourceId resourceId, out string reason)
            {
                reason = null;
                resourceId = default;

                if (string.IsNullOrEmpty(idToken))
                {
                    resourceId = Empty;
                    return true;
                }

                var match = ResourceIdRegEx.Match(idToken);
                if (!match.Success)
                {
                    reason = $"The id token format is invalid: '{idToken}'. The expected format is {IdFormat}";
                    return false;
                }

                // ResourceType
                var resourceTypeValue = match.Groups[ResourceTypeGroupName].Value;
                if (!Enum.TryParse<ResourceType>(resourceTypeValue, ignoreCase: true, out var resourceType))
                {
                    reason = $"Invalid {nameof(ResourceType)}: {resourceTypeValue}";
                    return false;
                }

                // InstanceId
                var instanceIdValue = match.Groups[InstanceIdGroupName].Value;
                if (!Guid.TryParse(instanceIdValue, out var instanceId))
                {
                    reason = $"Invalid instance id {nameof(Guid)}: {instanceIdValue}";
                    return false;
                }

                // SubscriptionId
                var subscriptionIdValue = match.Groups[SubscriptionIdGroupName].Value;
                if (!Guid.TryParse(subscriptionIdValue, out var subscriptionId))
                {
                    reason = $"Invalid subscription id {nameof(Guid)}: {subscriptionIdValue}";
                    return false;
                }

                // Location
                var locationValue = match.Groups[LocationGroupName].Value;
                if (!Enum.TryParse<AzureLocation>(locationValue, ignoreCase: true, out var location))
                {
                    reason = $"Invalid {nameof(AzureLocation)}: {locationValue}";
                    return false;
                }

                // ResourceGroup
                var resourceGroup = match.Groups[ResourceGroupGroupName].Value;
                if (string.IsNullOrEmpty(resourceGroup))
                {
                    reason = $"Invalid resourceGroup {nameof(String)}: {resourceGroup}";
                    return false;
                }

                // Construct and return a valid ResourceId
                resourceId = new ResourceId(resourceType, instanceId, subscriptionId, resourceGroup, location);
                return true;
            }
        }
    }
}
