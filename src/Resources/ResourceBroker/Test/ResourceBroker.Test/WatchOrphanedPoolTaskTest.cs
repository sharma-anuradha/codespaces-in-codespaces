using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class WatchOrphanedPoolTaskTest
    {
        private const AzureLocation DefaultLocation = AzureLocation.EastUs;
        private const AzureLocation WestLocation = AzureLocation.WestUs2;
        private const string DefaultResourceSkuName = "LargeVm";
        private const string DefaultLogicalSkuName = "Large";
        private const string StorageResourceSkuName = "LargeVm";
        private const ResourceType DefaultType = ResourceType.ComputeVM;

        private static IResourceContinuationOperations GetMockResourceContinuationOperations()
        {
            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            resourceContinuationOperations.
                Setup((x) => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), null))
                .Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            return resourceContinuationOperations.Object;
        }

        private static IDiagnosticsLogger GetMockDiagnosticsLogger()
        {
            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(l => l.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            return logger.Object;
        }

        [Fact]
        public void Ctor_ok()

        {
            var resourceBrokerSettings = new Mock<ResourceBrokerSettings>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var claimedDistributedLease = new Mock<IClaimedDistributedLease>().Object;
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var resourcePoolDefinitionStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourceContinuationOperations = GetMockResourceContinuationOperations();
            var configurationReader = new Mock<IConfigurationReader>().Object;
            
            var watchOrphanedPoolTask = new WatchOrphanedPoolTask(resourceBrokerSettings, resourceRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, resourcePoolDefinitionStore, resourceContinuationOperations, configurationReader);
            
            Assert.NotNull(watchOrphanedPoolTask);
        }

        [Fact]
        public void Ctor_throws_if_null()
        {
            var resourceBrokerSettings = new Mock<ResourceBrokerSettings>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var claimedDistributedLease = new Mock<IClaimedDistributedLease>().Object;
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var resourcePoolDefinitionStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var logger = GetMockDiagnosticsLogger();
            var resourceContinuationOperations = GetMockResourceContinuationOperations();
            var configurationReader = new Mock<IConfigurationReader>().Object;

            Assert.Throws<ArgumentNullException>(() => new WatchOrphanedPoolTask(null, resourceRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, resourcePoolDefinitionStore, resourceContinuationOperations, configurationReader));
            Assert.Throws<ArgumentNullException>(() => new WatchOrphanedPoolTask(resourceBrokerSettings, null, taskHelper, claimedDistributedLease, resourceNameBuilder, resourcePoolDefinitionStore, resourceContinuationOperations, configurationReader));
            Assert.Throws<ArgumentNullException>(() => new WatchOrphanedPoolTask(resourceBrokerSettings, resourceRepository, null, claimedDistributedLease, resourceNameBuilder, resourcePoolDefinitionStore, resourceContinuationOperations, configurationReader));
            Assert.Throws<ArgumentNullException>(() => new WatchOrphanedPoolTask(resourceBrokerSettings, resourceRepository, taskHelper, null, resourceNameBuilder, resourcePoolDefinitionStore, resourceContinuationOperations, configurationReader));
            Assert.Throws<ArgumentNullException>(() => new WatchOrphanedPoolTask(resourceBrokerSettings, resourceRepository, taskHelper, claimedDistributedLease, null, resourcePoolDefinitionStore, resourceContinuationOperations, configurationReader));
            Assert.Throws<ArgumentNullException>(() => new WatchOrphanedPoolTask(resourceBrokerSettings, resourceRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, null, resourceContinuationOperations, configurationReader));
            Assert.Throws<ArgumentNullException>(() => new WatchOrphanedPoolTask(resourceBrokerSettings, resourceRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, resourcePoolDefinitionStore, null, configurationReader));
        }

        [Fact]
        public async Task OrphanedPoolAsync()
        {
            var resourceContinuationOperations = GetMockResourceContinuationOperations();
            var resourcePoolDefinitionStoreMoq = BuildResourceScalingStore();
            var resourceBrokerSettings = new Mock<ResourceBrokerSettings>().Object;
            var claimedDistributedLease = new Mock<IClaimedDistributedLease>().Object;
            var resourceRepositoryMoq = new Mock<IResourceRepository>();
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var resourcePools = await resourcePoolDefinitionStoreMoq.Object.RetrieveDefinitionsAsync();
            var configurationReader = new Mock<IConfigurationReader>().Object;
            
            var unAssignedResourceRecord = new ResourceRecord
            {
                IsAssigned = false,
                IsDeleted = false,
                Id = "e8791146-b266-45a6-b5ac-8ed544a30107",
                PoolReference = new ResourcePoolDefinitionRecord()
                {
                    Code = "879008eb9d8896724ec87c9995d856decbe0379f",
                    VersionCode = "190f6fa867e62a6bdd3df8cb1c09a581485a4200",
                    Dimensions = new Dictionary<string, string>(),
                }
            };

            var assignedResourceRecord = new ResourceRecord
            {
                IsAssigned = true,
                IsDeleted = false,
                Id = "5ffa9426-9dd8-430a-8a33-9f99b3b5bdaf",
                PoolReference = new ResourcePoolDefinitionRecord()
                {
                    Code = "a68feb84c00f5ef2829776be4a92ea40b4c595c3",
                    VersionCode = "190f6fa867e62a6bdd3df8cb1c09a581485a4200",
                    Dimensions = new Dictionary<string, string>(),
                }
            };

            var watchOrphanedPoolTaskMoq = new Mock<WatchOrphanedPoolTask>(resourceBrokerSettings, resourceRepositoryMoq.Object, taskHelper, claimedDistributedLease, resourceNameBuilder, resourcePoolDefinitionStoreMoq.Object, resourceContinuationOperations, configurationReader);
            var watchOrphanedPoolTask = watchOrphanedPoolTaskMoq.Object;
            var Active = watchOrphanedPoolTask.IsActivePool(unAssignedResourceRecord.PoolReference.Code, resourcePools);
            var InActive = watchOrphanedPoolTask.IsActivePool(assignedResourceRecord.PoolReference.Code, resourcePools);

            // Resource that is active verified against resourcePools.
            Assert.True(Active);

            // Resource that is inactive verified against resourcePools.
            Assert.False(InActive);
        }

        [Fact]
        public async Task DeleteResourceAsync()
        {
            var resourceContinuationOperations = GetMockResourceContinuationOperations();
            var resourcePoolDefinitionStoreMoq = BuildResourceScalingStore();
            var logger = GetMockDiagnosticsLogger();
            var resourceBrokerSettings = new Mock<ResourceBrokerSettings>().Object;
            var claimedDistributedLease = new Mock<IClaimedDistributedLease>().Object;
            var resourceRepositoryMoq = new Mock<IResourceRepository>();
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var resourcePools = await resourcePoolDefinitionStoreMoq.Object.RetrieveDefinitionsAsync();
            var documentDbKeyPassed = default(DocumentDbKey);
            var resourceRecords = new List<ResourceRecord>();
            var configurationReader = new Mock<IConfigurationReader>().Object;
            
            var unAssignedResourceRecord = new ResourceRecord
            {
                IsAssigned = false,
                IsDeleted = false,
                Id = "e8791146-b266-45a6-b5ac-8ed544a30107",
                PoolReference = new ResourcePoolDefinitionRecord()
                {
                    Code = "879008eb9d8896724ec87c9995d856decbe0379f",
                    VersionCode = "190f6fa867e62a6bdd3df8cb1c09a581485a4200",
                    Dimensions = new Dictionary<string, string>(),
                }
            };

            var assignedResourceRecord = new ResourceRecord
            {
                IsAssigned = true,
                IsDeleted = false,
                Id = "5ffa9426-9dd8-430a-8a33-9f99b3b5bdaf",
                PoolReference = new ResourcePoolDefinitionRecord()
                {
                    Code = "a68feb84c00f5ef2829776be4a92ea40b4c595c3",
                    VersionCode = "190f6fa867e62a6bdd3df8cb1c09a581485a4200",
                    Dimensions = new Dictionary<string, string>(),
                }
            };

            var deletedResourceRecord = new ResourceRecord
            {
                IsAssigned = false,
                IsDeleted = true,
                Id = "5ffa9426-9dd8-430a-8a33-9f99b3b5bdaf",
                PoolReference = new ResourcePoolDefinitionRecord()
                {
                    Code = "123008eb9d8896724ec87c9995d856decbe0379f",
                    VersionCode = "190f6fa867e62a6bdd3df8cb1c09a581485a4200",
                    Dimensions = new Dictionary<string, string>(),
                }
            };

            resourceRecords.Add(unAssignedResourceRecord);
            resourceRecords.Add(assignedResourceRecord);
            resourceRecords.Add(deletedResourceRecord);

            // Getting an record from repo to check its most recent state before deletion.
            resourceRepositoryMoq
                .Setup(x => x.GetAsync(It.IsAny<DocumentDbKey>(), It.IsAny<IDiagnosticsLogger>()))
                .Callback<DocumentDbKey, IDiagnosticsLogger>((key, logger) => documentDbKeyPassed = key)
                .Returns(() =>
                {
                    var response = resourceRecords.Where((x) => x.Id == documentDbKeyPassed.Id);
                    return response.Count() > 0 ? Task.FromResult(response.First()) : null;
                });

            var watchOrphanedPoolTaskMoq = new Mock<WatchOrphanedPoolTask>(resourceBrokerSettings, resourceRepositoryMoq.Object, taskHelper, claimedDistributedLease, resourceNameBuilder, resourcePoolDefinitionStoreMoq.Object, resourceContinuationOperations, configurationReader);
            var watchOrphanedPoolTask = watchOrphanedPoolTaskMoq.Object;
            
            var response = watchOrphanedPoolTask.DeleteResourceAsync(assignedResourceRecord.Id, logger);
            
            // Active pool that is not getting deleted by worker.
            Assert.True(string.IsNullOrEmpty(response.Result));

            response = watchOrphanedPoolTask.DeleteResourceAsync(unAssignedResourceRecord.Id, logger);
            
            // Orphaned pool that gets deleted.
            Assert.True(!string.IsNullOrEmpty(response.Result));
            Assert.Equal(unAssignedResourceRecord.Id, response.Result);
        }

        private Mock<IResourcePoolDefinitionStore> BuildResourceScalingStore()
        {
            var acivePools = new List<ResourcePool>();
            {
                acivePools.Add(new ResourcePool { Id = "879008eb9d8896724ec87c9995d856decbe0379f", Details = new ResourcePoolComputeDetails { Location = DefaultLocation, SkuName = DefaultResourceSkuName }, Type = DefaultType, TargetCount = 10, LogicalSkus = new List<string> { DefaultLogicalSkuName } });
                acivePools.Add(new ResourcePool { Id = "c3d3a87287349029da7dd0e185c33a0563b3bb3a", Details = new ResourcePoolStorageDetails { Location = DefaultLocation, SkuName = StorageResourceSkuName }, Type = ResourceType.StorageFileShare, TargetCount = 10, LogicalSkus = new List<string> { DefaultLogicalSkuName } });
                acivePools.Add(new ResourcePool { Id = "e906f19991035bf4191298d159733231f3b41143", Details = new ResourcePoolComputeDetails { Location = WestLocation, SkuName = DefaultResourceSkuName }, Type = DefaultType, TargetCount = 10, LogicalSkus = new List<string> { DefaultLogicalSkuName } });
            }

            var resourceScalingStore = new Mock<IResourcePoolDefinitionStore>();
            
            resourceScalingStore.Setup(x => x.RetrieveDefinitionsAsync())
                                .Returns(Task.FromResult((IList<ResourcePool>)acivePools));

            return resourceScalingStore;
        }
    }
}
