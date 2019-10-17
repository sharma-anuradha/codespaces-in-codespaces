// <copyright file="IPlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    public interface IPlanRepository : IDocumentDbCollection<VsoPlan>
    {
    }
}
