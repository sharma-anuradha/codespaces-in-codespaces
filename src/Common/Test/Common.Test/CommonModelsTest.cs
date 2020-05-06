using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class CommonModelsTest
    {
        [Fact]
        public void ResourceComponent_Simple_ctor_Success()
        {
            Assert.NotNull(new ResourceComponent());
            Assert.NotNull(new ResourceComponent(ComponentType.OSDisk, null));
            Assert.NotNull(new ResourceComponent(ComponentType.OSDisk, null, null));
            Assert.NotNull(new ResourceComponent(ComponentType.OSDisk, new AzureResourceInfo(), null));
        }

        [Fact]
        public void ResourceComponent_Equals_SameType()
        {
            var rc1 = new ResourceComponent(ComponentType.OSDisk, null);
            var rc2 = new ResourceComponent(ComponentType.OSDisk, null);
            Assert.True(rc1.Equals(rc2));
        }

        [Fact]
        public void ResourceComponent_Equals_SameType_Different_Resource()
        {
            var rc1 = new ResourceComponent(ComponentType.OSDisk, null, Guid.NewGuid().ToString());
            var rc2 = new ResourceComponent(ComponentType.OSDisk, null, Guid.NewGuid().ToString());
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

            var rc1 = new ResourceComponent(ComponentType.OSDisk, azResource1);
            var rc2 = new ResourceComponent(ComponentType.OSDisk, azResource2);
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

        [Fact]
        public void AzureResourceInfo_Equals_Same_WithComponents()
        {
            var guid = Guid.NewGuid();
            var rg = "SomeRg";
            var name = "SomeName";
            var componentAzRes = new AzureResourceInfo(Guid.NewGuid(), "Rg", "Rcn");
            var component = new ResourceComponent(ComponentType.NetworkInterface, componentAzRes);

            var azResource1 = new AzureResourceInfo(guid, rg, name)
            {
                Components = new List<ResourceComponent> { component }
            };

            var azResource2 = new AzureResourceInfo(guid, rg, name)
            {
                Components = new List<ResourceComponent> { component }
            };

            Assert.True(azResource1.Equals(azResource2));
            Assert.True(azResource2.Equals(azResource1));
        }

        [Fact]
        public void AzureResourceInfo_Equals_Fail_Different_Components()
        {
            var guid = Guid.NewGuid();
            var rg = "SomeRg";
            var name = "SomeName";
            var componentAzRes1 = new AzureResourceInfo(Guid.NewGuid(), "Rg1", "Rcn1");
            var component1 = new ResourceComponent(ComponentType.NetworkInterface, componentAzRes1);

            var componentAzRes2 = new AzureResourceInfo(Guid.NewGuid(), "Rg2", "Rcn2");
            var component2 = new ResourceComponent(ComponentType.NetworkInterface, componentAzRes2);

            var azResource1 = new AzureResourceInfo(guid, rg, name)
            {
                Components = new List<ResourceComponent> { component1 }
            };

            var azResource2 = new AzureResourceInfo(guid, rg, name)
            {
                Components = new List<ResourceComponent> { component2 }
            };

            Assert.False(azResource1.Equals(azResource2));
            Assert.False(azResource2.Equals(azResource1));
        }

        [Fact]
        public void AzureResourceInfo_Equals_Fail_Different_Components_Count()
        {
            var guid = Guid.NewGuid();
            var rg = "SomeRg";
            var name = "SomeName";
            var componentAzRes1 = new AzureResourceInfo(Guid.NewGuid(), "Rg1", "Rcn1");
            var component1 = new ResourceComponent(ComponentType.NetworkInterface, componentAzRes1);

            var componentAzRes2 = new AzureResourceInfo(Guid.NewGuid(), "Rg2", "Rcn2");
            var component2 = new ResourceComponent(ComponentType.NetworkInterface, componentAzRes2);

            var azResource1 = new AzureResourceInfo(guid, rg, name)
            {
                Components = new List<ResourceComponent> { component1 }
            };

            var azResource2 = new AzureResourceInfo(guid, rg, name)
            {
                Components = new List<ResourceComponent> { component1, component2 }
            };

            Assert.False(azResource1.Equals(azResource2));
            Assert.False(azResource2.Equals(azResource1));
        }
    }
}
