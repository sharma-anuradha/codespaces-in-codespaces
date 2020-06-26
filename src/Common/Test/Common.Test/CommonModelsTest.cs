// <copyright file="CommonModelsTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class CommonModelsTest
    {
        [Fact]
        public void ResourceComponent_Simple_ctor_Success()
        {
            Assert.NotNull(new ResourceComponent());
            Assert.NotNull(new ResourceComponent(ResourceType.OSDisk, null));
            Assert.NotNull(new ResourceComponent(ResourceType.OSDisk, null, null));
            Assert.NotNull(new ResourceComponent(ResourceType.OSDisk, new AzureResourceInfo(), null));
        }

        [Fact]
        public void ResourceComponent_Equals_SameType()
        {
            var rc1 = new ResourceComponent(ResourceType.OSDisk, null);
            var rc2 = new ResourceComponent(ResourceType.OSDisk, null);
            Assert.True(rc1.Equals(rc2));
        }

        [Fact]
        public void ResourceComponent_Equals_SameType_Different_Resource()
        {
            var rc1 = new ResourceComponent(ResourceType.OSDisk, null, Guid.NewGuid().ToString());
            var rc2 = new ResourceComponent(ResourceType.OSDisk, null, Guid.NewGuid().ToString());
            Assert.False(rc1.Equals(rc2));
        }

        [Fact]
        public void ResourceComponent_Equals_With_Azure_Resource()
        {
            var guid = Guid.NewGuid();
            var rg = "SomeRg";
            var name = "SomeName";
            var azResource1 = new AzureResourceInfo(guid, rg, name);
            var azResource2 = new AzureResourceInfo(guid, rg, name);

            var rc1 = new ResourceComponent(ResourceType.OSDisk, azResource1);
            var rc2 = new ResourceComponent(ResourceType.OSDisk, azResource2);
            Assert.True(rc1.Equals(rc2));
        }

        [Fact]
        public void AzureResourceInfo_Simple_ctor_Success()
        {
            Assert.NotNull(new AzureResourceInfo());
            Assert.NotNull(new AzureResourceInfo(Guid.NewGuid(), "SomeRg", "SomeName"));
            Assert.NotNull(new AzureResourceInfo(Guid.NewGuid().ToString(), "SomeRg", "SomeName"));
        }

        [Fact]
        public void AzureResourceInfo_ctor_ThrowsException()
        {
            Assert.Throws<FormatException>(() => new AzureResourceInfo("NotAGuid", "SomeRg", "SomeName"));
            Assert.Throws<ArgumentException>(() => new AzureResourceInfo(Guid.NewGuid(), "", "SomeName"));
            Assert.Throws<ArgumentException>(() => new AzureResourceInfo(Guid.NewGuid(), "SomeRg", ""));
            Assert.Throws<ArgumentNullException>(() => new AzureResourceInfo(Guid.NewGuid(), "SomeRg", null));
            Assert.Throws<ArgumentNullException>(() => new AzureResourceInfo(Guid.NewGuid(), null, "SomeName"));
        }

        [Fact]
        public void AzureResourceInfo_Equals_Same()
        {
            var guid = Guid.NewGuid();
            var rg = "SomeRg";
            var name = "SomeName";
            var azResource1 = new AzureResourceInfo(guid, rg, name);
            var azResource2 = new AzureResourceInfo(guid, rg, name);
            Assert.True(azResource1.Equals(azResource2));
            Assert.True(azResource2.Equals(azResource1));
        }
    }
}
