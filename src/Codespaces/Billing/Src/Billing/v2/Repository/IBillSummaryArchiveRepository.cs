// <copyright file="IBillSummaryArchiveRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository
{
    /// <summary>
    /// the interface for the billing summary repository.
    /// </summary>
    public interface IBillSummaryArchiveRepository : IDocumentDbCollection<BillSummary>
    {
    }
}
