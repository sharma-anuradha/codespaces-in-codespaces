// <copyright file="IEnvironmentGetAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Get Action.
    /// </summary>
    public interface IEnvironmentGetAction : IEnvironmentItemAction<Guid>
    {
    }
}
