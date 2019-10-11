// <copyright file="StorageFileShareProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Test
{
    public class StorageFileShareProviderTests
    {
        // private const string MockResourceId = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/TestRg1/providers/Microsoft.Storage/storageAccounts/TestStorageAccount";
        private const string MockResourceGroup = "TestRg1";
        private const string MockLocation = "westus2";
        private const string MockStorageBlobUrl = "https://staccname.blob.core.windows.net/containername/blobname";
        private const string MockStorageAccountName = "vsoce123";
        private const string MockStorageAccountKey = "SecretKey";
        private const string MockStorageShareName = "cloudenvdata";
        private const string MockStorageFileName = "dockerlib";
        private static readonly Guid MockSubscriptionId = Guid.Parse("a058a07c-dfbb-4501-82a2-fa0bb37ec166");
        private static readonly AzureResourceInfo MockAzureResourceInfo = new AzureResourceInfo(MockSubscriptionId, MockResourceGroup, MockStorageAccountName);
        private static readonly PrepareFileShareTaskInfo MockPrepareTaskInfo = new PrepareFileShareTaskInfo("job1", "task1", MockLocation);

        private static readonly IDictionary<string, string> MockResourceTags = new Dictionary<string, string>
        {
            {"ResourceTag", "GeneratedFromTest"},
        };

        [Fact]
        public void Ctor_with_bad_options()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageFileShareProvider(null));
        }

        /// <summary>
        /// Create operation succeeds.
        /// </summary>
        [Fact]
        public async Task FileShare_Create_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            var mockCheckPrepareResults = new[] {
                PrepareFileShareStatus.Pending,
                PrepareFileShareStatus.Running,
                PrepareFileShareStatus.Running,
                PrepareFileShareStatus.Succeeded,
             };
            providerHelperMoq
                .Setup(x => x.CreateStorageAccountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(MockAzureResourceInfo);
            providerHelperMoq
                .Setup(x => x.CreateFileShareAsync(It.IsAny<AzureResourceInfo>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.CompletedTask);
            providerHelperMoq
                .Setup(x => x.StartPrepareFileShareAsync(It.IsAny<AzureResourceInfo>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(MockPrepareTaskInfo);

            providerHelperMoq
                .SetupSequence(x => x.CheckPrepareFileShareAsync(It.IsAny<AzureResourceInfo>(), It.IsAny<PrepareFileShareTaskInfo>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(mockCheckPrepareResults[0])
                .ReturnsAsync(mockCheckPrepareResults[1])
                .ReturnsAsync(mockCheckPrepareResults[2])
                .ReturnsAsync(mockCheckPrepareResults[3]);

            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);

            var input = new FileShareProviderCreateInput()
            {
                AzureResourceGroup = MockResourceGroup,
                AzureLocation = MockLocation,
                AzureSubscription = MockSubscriptionId.ToString(),
                StorageBlobUrl = MockStorageBlobUrl,
                ResourceTags = MockResourceTags,
            };

            // 3 because there are 3 steps before we wait for the preparation to complete.
            // 1. Create Storage Account
            // 2. Create File Share
            // 3. Start Prepare File Share
            // Wait for preparation to complete...
            int expectedIterations = 3 + mockCheckPrepareResults.Length;

            for (int iteration = 1; iteration <= expectedIterations; iteration++)
            {
                if (iteration == 1)
                {
                    // At the beginning, continuation token should be null.
                    Assert.Null(input.ContinuationToken);
                }
                var result = await storageProvider.CreateAsync(input, logger);
                // result should not be null after each iteration
                Assert.NotNull(result);
                // Resource info should not be null after each iteration
                Assert.NotNull(result.AzureResourceInfo);
                var continuationToken = result.NextInput?.ContinuationToken;
                input = (FileShareProviderCreateInput)result.NextInput;
                // On final iteration, operation state should be succeeded with null continuation token, otherwise, in progress.
                if (iteration == expectedIterations)
                {
                    Assert.Equal(OperationState.Succeeded, result.Status);
                    Assert.Null(continuationToken);
                }
                else
                {
                    Assert.Equal(OperationState.InProgress, result.Status);
                }
            }
        }

        /// <summary>
        /// Create operation that failed should return null continuation token and failed status.
        /// </summary>
        [Fact]
        public async Task FileShare_Create_Failed()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            providerHelperMoq
                .Setup(x => x.CreateStorageAccountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<IDiagnosticsLogger>()))
                .Throws(new Exception());
            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);
            var input = new FileShareProviderCreateInput()
            {
                AzureResourceGroup = MockResourceGroup,
                AzureLocation = MockLocation,
                AzureSubscription = MockSubscriptionId.ToString(),
                StorageBlobUrl = MockStorageBlobUrl,
                ResourceTags = MockResourceTags,
            };
            var result = await storageProvider.CreateAsync(input, logger);
            Assert.NotNull(result);
            Assert.Null(result.NextInput);
            Assert.Equal(OperationState.Failed, result.Status);
        }

        /// <summary>
        /// Delete operation succeeds.
        /// We check that there is no continuation token at the end as Delete is a one step operation.
        /// </summary>
        [Fact]
        public async Task FileShare_Delete_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            providerHelperMoq
                .Setup(x => x.DeleteStorageAccountAsync(It.IsAny<AzureResourceInfo>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.CompletedTask);
            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);
            var input = new FileShareProviderDeleteInput()
            {
                AzureResourceInfo = MockAzureResourceInfo,
            };
            var result = await storageProvider.DeleteAsync(input, logger);
            Assert.NotNull(result);
            Assert.Null(result.NextInput);
            Assert.Equal(OperationState.Succeeded, result.Status);
        }

        /// <summary>
        /// Delete operation raise an exception on null input.
        /// </summary>
        [Fact]
        public async Task FileShare_Delete_Null_Input()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);
            FileShareProviderDeleteInput input = null;
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await storageProvider.DeleteAsync(input, logger));
        }

        /// <summary>
        /// Delete operation that failed should return null continuation token and failed status.
        /// </summary>
        [Fact]
        public async Task FileShare_Delete_Failed()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            providerHelperMoq
                .Setup(x => x.DeleteStorageAccountAsync(It.IsAny<AzureResourceInfo>(), It.IsAny<IDiagnosticsLogger>()))
                .Throws(new Exception());
            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);
            FileShareProviderDeleteInput input = new FileShareProviderDeleteInput()
            {
                AzureResourceInfo = MockAzureResourceInfo,
            };
            var result = await storageProvider.DeleteAsync(input, logger);
            Assert.NotNull(result);
            Assert.Null(result.NextInput);
            Assert.Equal(OperationState.Failed, result.Status);
        }

        /// <summary>
        /// Assign operation succeeds and returns the correct connection info.
        /// We check that there is no continuation token at the end as Delete is a one step operation.
        /// </summary>
        [Fact]
        public async Task FileShare_Assign_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            var mockConnInfo = new ShareConnectionInfo(
                MockStorageAccountName,
                MockStorageAccountKey,
                MockStorageShareName,
                MockStorageFileName);
            providerHelperMoq
                .Setup(x => x.GetConnectionInfoAsync(It.IsAny<AzureResourceInfo>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockConnInfo));
            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);
            var input = new FileShareProviderAssignInput()
            {
                AzureResourceInfo = MockAzureResourceInfo,
            };
            var result = await storageProvider.AssignAsync(input, logger);
            Assert.NotNull(result);
            Assert.Equal(MockStorageAccountName, result.StorageAccountName);
            Assert.Equal(MockStorageAccountKey, result.StorageAccountKey);
            Assert.Equal(MockStorageShareName, result.StorageShareName);
            Assert.Equal(MockStorageFileName, result.StorageFileName);
            Assert.Null(result.NextInput);
            Assert.Equal(OperationState.Succeeded, result.Status);
        }

        /// <summary>
        /// Assign operation raise an exception on null input.
        /// </summary>
        [Fact]
        public async Task FileShare_Assign_Null_Input()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);
            FileShareProviderAssignInput input = null;
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await storageProvider.AssignAsync(input, logger));
        }

        /// <summary>
        /// Assign operation that failed should return null continuation token and failed status.
        /// </summary>
        [Fact]
        public async Task FileShare_Assign_Failed()
        {
            var logger = new DefaultLoggerFactory().New();
            var providerHelperMoq = new Mock<IStorageFileShareProviderHelper>();
            providerHelperMoq
                .Setup(x => x.GetConnectionInfoAsync(It.IsAny<AzureResourceInfo>(), It.IsAny<IDiagnosticsLogger>()))
                .Throws(new Exception());
            var storageProvider = new StorageFileShareProvider(providerHelperMoq.Object);
            FileShareProviderAssignInput input = new FileShareProviderAssignInput()
            {
                AzureResourceInfo = MockAzureResourceInfo,
            };
            var result = await storageProvider.AssignAsync(input, logger);
            Assert.NotNull(result);
            Assert.Null(result.NextInput);
            Assert.Equal(OperationState.Failed, result.Status);
        }
    }
}
