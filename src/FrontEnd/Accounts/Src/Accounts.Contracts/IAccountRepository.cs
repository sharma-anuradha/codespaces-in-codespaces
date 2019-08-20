// <copyright file="IAccountRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    public interface IAccountRepository : IDocumentDbCollection<VsoAccount>
    {
    }
}
