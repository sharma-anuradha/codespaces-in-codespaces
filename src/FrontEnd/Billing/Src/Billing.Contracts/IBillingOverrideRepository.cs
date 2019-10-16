// <copyright file="IBillingOverrideRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Interface for our billing override datastore
    /// </summary>
    public interface IBillingOverrideRepository : IDocumentDbCollection<BillingOverride>
    {
    }
}
