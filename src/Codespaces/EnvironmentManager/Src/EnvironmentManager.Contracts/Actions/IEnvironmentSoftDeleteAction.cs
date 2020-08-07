// <copyright file="IEnvironmentSoftDeleteAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Soft Delete Action.
    /// </summary>
    public interface IEnvironmentSoftDeleteAction : IEnvironmentBaseItemAction<Guid, object, CloudEnvironment>
    {
    }
}
