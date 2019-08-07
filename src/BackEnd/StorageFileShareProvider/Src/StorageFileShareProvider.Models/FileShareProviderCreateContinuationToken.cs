// <copyright file="FileShareProviderCreateContinuationToken.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Represents the continuation token for the create operation.
    /// </summary>
    public class FileShareProviderCreateContinuationToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareProviderCreateContinuationToken"/> class.
        /// </summary>
        /// <param name="nextState"><see cref="NextState"/>.</param>
        /// <param name="resourceId"><see cref="ResourceId"/>.</param>
        public FileShareProviderCreateContinuationToken(FileShareProviderCreateState nextState, string resourceId)
        {
            NextState = nextState;
            ResourceId = resourceId;
        }

        /// <summary>
        /// Gets the next state of the operation.
        /// </summary>
        public FileShareProviderCreateState NextState { get; }

        /// <summary>
        /// Gets the resource id.
        /// </summary>
        public string ResourceId { get; }
    }
}
