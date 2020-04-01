// <copyright file="ICrossRegionContinuationJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Resource queue collection interface for cross region message passing.
    /// </summary>
    public interface ICrossRegionContinuationJobQueueRepository : ICrossRegionStorageQueueCollection
    {
    }
}
