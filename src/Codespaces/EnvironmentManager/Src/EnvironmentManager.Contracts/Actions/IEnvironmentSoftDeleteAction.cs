// <copyright file="IEnvironmentSoftDeleteAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Soft Delete Action.
    /// </summary>
    public interface IEnvironmentSoftDeleteAction : IEnvironmentBaseItemAction<Guid, object, bool>
    {
    }
}
