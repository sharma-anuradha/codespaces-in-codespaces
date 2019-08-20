// <copyright file="MockBillingEventRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class MockBillingEventRepository : MockRepository<BillingEvent>, IBillingEventRepository
    {
    }
}
