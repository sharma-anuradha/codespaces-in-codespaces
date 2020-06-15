// <copyright file="FileShareProviderArchiveContinuationToken.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Represents the continuation token for the create operation.
    /// </summary>
    public class FileShareProviderArchiveContinuationToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareProviderArchiveContinuationToken"/> class.
        /// </summary>
        /// <param name="nextState"><see cref="NextState"/>.</param>
        /// <param name="azureResourceInfo">The resource info of the azure storage account.</param>
        /// <param name="prepareTaskInfo">The info for the task preparing the file share.</param>
        public FileShareProviderArchiveContinuationToken(FileShareProviderArchiveState nextState, AzureResourceInfo azureResourceInfo, BatchTaskInfo prepareTaskInfo)
        {
            NextState = nextState;
            AzureResourceInfo = azureResourceInfo;
            PrepareTaskInfo = prepareTaskInfo;
        }

        /// <summary>
        /// Gets the next state of the operation.
        /// </summary>
        public FileShareProviderArchiveState NextState { get; }

        /// <summary>
        /// Gets the resource info of the azure storage account.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; }

        /// <summary>
        /// Gets the info for the task preparing the file share.
        /// </summary>
        public BatchTaskInfo PrepareTaskInfo { get; }
    }
}
