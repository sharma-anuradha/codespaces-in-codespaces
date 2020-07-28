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
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    public interface IEnvironmentItemAction<TInput, TState> : IEntityAction<TInput, TState, CloudEnvironment>
    {
    }
}
