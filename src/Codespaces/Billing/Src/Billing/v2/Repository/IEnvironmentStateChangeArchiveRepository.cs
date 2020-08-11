// <copyright file="IEnvironmentStateChangeArchiveRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository
{
    /// <summary>
    /// the interface for the billing summary repository.
    /// </summary>
    public interface IEnvironmentStateChangeArchiveRepository : IDocumentDbCollection<EnvironmentStateChange>
    {
    }
}
