// <copyright file="MockPlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    public class MockPlanRepository : MockRepository<VsoPlan>, IPlanRepository
    {
    }
}
