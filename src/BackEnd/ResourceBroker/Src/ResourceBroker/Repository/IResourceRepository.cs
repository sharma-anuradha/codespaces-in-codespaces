// <copyright file="IResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository
{
    /// <summary>
    ///
    /// </summary>
    public interface IResourceRepository : IDocumentDbCollection<ResourceRecord>
    {
        Task<ResourceRecord> GetPoolReadyUnassignedAsync(string poolCode, IDiagnosticsLogger logger);

        Task<int> GetPoolUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger);

        Task<int> GetPoolReadyUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger);

        Task<int> GetPoolUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<int> GetPoolReadyUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<int> GetPoolUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<int> GetPoolReadyUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<IEnumerable<string>> GetPoolUnassignedAsync(string poolCode, int count, IDiagnosticsLogger logger);

        Task<IEnumerable<string>> GetPoolUnassignedNotVersionAsync(string poolCode, string poolVersionCode, int count, IDiagnosticsLogger logger);
    }
}
