// <copyright file="IPlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    public interface IPlanRepository : IDocumentDbCollection<VsoPlan>
    {
        Task<int> GetCountAsync(IDiagnosticsLogger logger);
    }
}
