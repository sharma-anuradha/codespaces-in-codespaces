// <copyright file="IEnvironmentForceSuspendAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment force suspend action.
    /// </summary>
    public interface IEnvironmentForceSuspendAction : IEnvironmentItemAction<Guid, object>
    {
    }
}
