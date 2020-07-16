// <copyright file="IEnvironmentItemAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Item Action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    public interface IEnvironmentItemAction<TInput> : IEntityAction<TInput, CloudEnvironment>
    {
    }
}
