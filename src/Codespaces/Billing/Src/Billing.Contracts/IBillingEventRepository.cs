// <copyright file="IBillingEventRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public interface IBillingEventRepository : IDocumentDbCollection<BillingEvent>
    {
    }
}
