// <copyright file="MockBillingEventRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A mock doc DB container for deveolopment/testing.
    /// </summary>
    public class MockBillingEventRepository : MockRepository<BillingEvent>, IBillingEventRepository
    {
    }
}
