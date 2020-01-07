// <copyright file="MockBillingOverrideRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A mock DocDB container for billing overrides for development/testing.
    /// </summary>
    public class MockBillingOverrideRepository : MockRepository<BillingOverride>, IBillingOverrideRepository
    {
    }
}
