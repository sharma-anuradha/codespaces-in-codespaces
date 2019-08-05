// <copyright file="IResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skuName"></param>
        /// <param name="type"></param>
        /// <param name="location"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<ResourceRecord> GetUnassignedResourceAsync(string skuName, ResourceType type, string location, IDiagnosticsLogger logger);
    }
}
