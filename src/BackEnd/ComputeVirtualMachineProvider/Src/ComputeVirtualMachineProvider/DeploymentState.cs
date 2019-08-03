// <copyright file="DeploymentState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public enum DeploymentState
    {
        Succeeded = 1,
        Failed = 2,
        Cancelled = 3,
        InProgress = 4,
    }
}