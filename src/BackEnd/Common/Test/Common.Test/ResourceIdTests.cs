// <copyright file="ResourceIdTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class ResourceIdTests
    {
        [Fact]
        public void Empty()
        {
            // Empty from default field initializers
            var empty = default(ResourceId);
            Assert.Equal(Guid.Empty, empty.InstanceId);
            Assert.Equal(Guid.Empty, empty.SubscriptionId);
            Assert.Equal(default, empty.ResourceType);
            Assert.Equal(default, empty.Location);

            // Empty from static field
            empty = ResourceId.Empty;
            Assert.Equal(Guid.Empty, empty.InstanceId);
            Assert.Equal(Guid.Empty, empty.SubscriptionId);
            Assert.Equal(default, empty.ResourceType);
            Assert.Equal(default, empty.Location);

            // Empty from constructor
            empty = new ResourceId(default, default, default, default, default);
            Assert.Equal(Guid.Empty, empty.InstanceId);
            Assert.Equal(Guid.Empty, empty.SubscriptionId);
            Assert.Equal(default, empty.ResourceType);
            Assert.Equal(default, empty.Location);

            // Empty from Parse
            empty = ResourceId.Parse(null);
            Assert.Equal(Guid.Empty, empty.InstanceId);
            Assert.Equal(Guid.Empty, empty.SubscriptionId);
            Assert.Equal(default, empty.ResourceType);
            Assert.Equal(default, empty.Location);

            // Empty from Parse
            empty = ResourceId.Parse(string.Empty);
            Assert.Equal(Guid.Empty, empty.InstanceId);
            Assert.Equal(Guid.Empty, empty.SubscriptionId);
            Assert.Equal(default, empty.ResourceType);
            Assert.Equal(default, empty.Location);
        }

        [Fact]
        public void Ctor_OK()
        {
            var subscriptionId = Guid.NewGuid();
            var resourceGroup = Guid.NewGuid().ToString();
            var instanceId = Guid.NewGuid();
            var location = AzureLocation.AustraliaCentral;
            var id = new ResourceId(ResourceType.ComputeVM, instanceId, subscriptionId, resourceGroup, location);
            Assert.Equal(ResourceType.ComputeVM, id.ResourceType);
            Assert.Equal(instanceId, id.InstanceId);
            Assert.Equal(subscriptionId, id.SubscriptionId);
            Assert.Equal(location, id.Location);
        }

        [Fact]
        public void Ctor_Empty()
        {
            var empty = new ResourceId(default, default, default, default, default);
            Assert.Equal(default, empty.ResourceType);
            Assert.Equal(default, empty.InstanceId);
            Assert.Equal(default, empty.SubscriptionId);
            Assert.Equal(default, empty.Location);
        }

        [Fact]
        public void Ctor_Throws()
        {
            var resourceType = ResourceType.ComputeVM;
            var instanceId = Guid.NewGuid();
            var subscriptionId = Guid.NewGuid();
            var resourceGroup = Guid.NewGuid().ToString();
            var location = AzureLocation.AustraliaCentral;

            Assert.Throws<ArgumentException>("resourceType", () => new ResourceId(default, instanceId, subscriptionId, resourceGroup, location));
            Assert.Throws<ArgumentException>("instanceId", () => new ResourceId(resourceType, default, subscriptionId, resourceGroup, location));
            Assert.Throws<ArgumentException>("subscriptionId", () => new ResourceId(resourceType, instanceId, default, resourceGroup, location));
            Assert.Throws<ArgumentException>("location", () => new ResourceId(resourceType, instanceId, subscriptionId, default, location));
            Assert.Throws<ArgumentException>("location", () => new ResourceId(resourceType, instanceId, subscriptionId, resourceGroup, default));
        }

        [Fact]
        public void Equality_Empty_Is_Empty()
        {
            var empty1 = ResourceId.Empty;
            var empty2 = ResourceId.Empty;
            Assert.True(Equals(empty1, empty2));
            Assert.True(Equals(empty2, empty1));
            Assert.Equal(empty1, empty2);
            Assert.Equal(empty2, empty1);
            Assert.True(empty1 == empty2);
            Assert.True(empty2 == empty1);
            Assert.True(empty1.Equals(empty2));
            Assert.True(empty2.Equals(empty1));
            Assert.Equal(empty1.GetHashCode(), empty2.GetHashCode());
            Assert.Equal(0, empty1.GetHashCode());
        }

        [Fact]
        public void Equality_Equals()
        {
            var id1 = NewTestResourceId();
            var id2 = new ResourceId(id1.ResourceType, id1.InstanceId, id1.SubscriptionId, id1.ResourceGroup, id1.Location);
            Assert.True(Equals(id1, id2));
            Assert.True(Equals(id2, id1));
            Assert.Equal(id1, id2);
            Assert.Equal(id2, id1);
            Assert.True(id1 == id2);
            Assert.True(id2 == id1);
            Assert.True(id1.Equals(id2));
            Assert.True(id2.Equals(id1));
            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
        }

        [Fact]
        public void Equality_Not_Equals()
        {
            var id1 = NewTestResourceId();
            var id2 = NewTestResourceId();
            Assert.False(Equals(id1, id2));
            Assert.False(Equals(id2, id1));
            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id2, id1);
            Assert.True(id1 != id2);
            Assert.True(id2 != id1);
            Assert.False(id1.Equals(id2));
            Assert.False(id2.Equals(id1));
            Assert.NotEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [Fact]
        public void Equality_Null_Not_Equals()
        {
            var id = NewTestResourceId();
            Assert.False(Equals(id, null));
            Assert.False(Equals(null, id));
            Assert.True(id != null);
            Assert.True(null != id);
            Assert.False(id.Equals(null));
        }

        [Fact]
        public void Equality_Not_Null()
        {
            var id = NewTestResourceId();
            Assert.False(Equals(id, null));
            Assert.False(Equals(null, id));
            Assert.True(id != null);
            Assert.True(null != id);
            Assert.False(id.Equals(null));
        }

        [Fact]
        public void ToString_Format()
        {
            var id = NewTestResourceId();
            var idToken = id.ToString();
            // Specific format
            var expectedLocation = id.Location.ToString().ToLowerInvariant();
            Assert.Equal($"vssaas/resourcetypes/computevm/instances/{id.InstanceId}/subscriptions/{id.SubscriptionId}/locations/{expectedLocation}", idToken);
        }

        [Fact]
        public void ToString_Is_Empty()
        {
            Assert.Equal(string.Empty, ResourceId.Empty.ToString());
            Assert.Equal(string.Empty, (string)ResourceId.Empty);
        }

        [Fact]
        public void Parse_RoundTrip_OK()
        {
            var id1 = NewTestResourceId();
            var id2 = ResourceId.Parse(id1);
            Assert.Equal(id1, id2);
            Assert.True(ResourceId.TryParse(id1, out id2));
            Assert.Equal(id1, id2);
        }

        private const string InstanceId = "ebeea9c1-6898-4abb-b76f-ad087add2bda";
        private const string SubscriptionId = "34da0f9b-78b3-4158-b1e9-0823f728fcf3";
        public static TheoryData ParseData =>
            new TheoryData<int, string, bool>
            {
                // The first columun is a test id number for identifying test failures
                { 0, null, true },
                { 1, string.Empty, true },
                { 2, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus", true},
                { 3, " ", false },
                { 4, "garbage", false },
                { 5, $" vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus", false},
                { 6, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus ", false},
                { 7, $"vs-saas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus", false},
                { 8, $"vssaas/resource-types/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus", false},
                { 9, $"vssaas/resourcetypes/compute-vm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus", false},
                { 10, $"vssaas/resourcetypes/computevm/Instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus", true},
                { 11, $"vssaas/resourcetypes/computevm/instances/{InstanceId}-/subscriptions/{SubscriptionId}/locations/eastus", false},
                { 12, $"vssaas/resourcetypes/computevm/instances/{InstanceId}X/subscriptions/{SubscriptionId}/locations/eastus", false},
                { 13, $"vssaas/resourcetypes/computevm/instances/{InstanceId}!/subscriptions/{SubscriptionId}/locations/eastus", false},
                { 14, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/Subscriptions/{SubscriptionId}/locations/eastus", true},
                { 15, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}-/locations/eastus", false},
                { 16, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}X/locations/eastus", false},
                { 17, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}!/locations/eastus", false},
                { 18, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/Locations/eastus", true},
                { 19, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/east-us", false},
                { 20, $"vssaas/resourcetypes/computevm/instances/{InstanceId}/subscriptions/{SubscriptionId}/locations/eastus!", false},
            };

        [Theory]
        [MemberData(nameof(ParseData))]
        public void Parse(int num, string id, bool ok)
        {
            _ = num;

            if (ok)
            {
                _ = ResourceId.Parse(id);
                Assert.True(ResourceId.TryParse(id, out _));
            }
            else
            {
                Assert.Throws<FormatException>(() => _ = ResourceId.Parse(id));
                Assert.False(ResourceId.TryParse(id, out _));
            }
        }

        [Fact]
        public void ImplicitOperatorString()
        {
            var resourceId = NewTestResourceId();
            var idToken = resourceId.ToString();
            string implicitConversion = resourceId;
            Assert.Equal(idToken, implicitConversion);
        }

        [Fact]
        public void ExplicitOperatorString()
        {
            var resourceId = NewTestResourceId();
            var idToken = resourceId.ToString();
            var explicitConversion = (string)resourceId;
            Assert.Equal(idToken, explicitConversion);
        }

        private static ResourceId NewTestResourceId()
        {
            return new ResourceId(ResourceType.ComputeVM, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString(), AzureLocation.AustraliaCentral);
        }
    }
}
