// <copyright file="IEnvironmentBaseStartAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Item Base Start Action. Supports resuming and exporting an environment.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    public interface IEnvironmentBaseStartAction<TInput, TState> : IEnvironmentItemAction<TInput, TState>
    {
    }
}
