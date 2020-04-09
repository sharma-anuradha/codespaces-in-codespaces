// <copyright file="IWatchOrphanedComputeImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// IWatchOrphanedComputeImagesTask to delete obsolete artifacts (Nexus windows images/blobs).
    /// </summary>
    public interface IWatchOrphanedComputeImagesTask : IBackgroundTask
    {
        /// <summary>
        /// Gets all the Images/Blobs that are being currently used form SkuCatalog.
        /// </summary>
        /// <param name="logger">The logger which should be used.</param>
        /// <returns>A Task representing the completion of the processing logic.</returns>
        Task<IEnumerable<string>> GetActiveImageVersionsAsync(IDiagnosticsLogger logger);
    }
}
