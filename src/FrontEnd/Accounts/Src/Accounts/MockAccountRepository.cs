// <copyright file="MockAccountRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    public class MockAccountRepository : MockRepository<VsoAccount>, IAccountRepository
    {
    }
}
