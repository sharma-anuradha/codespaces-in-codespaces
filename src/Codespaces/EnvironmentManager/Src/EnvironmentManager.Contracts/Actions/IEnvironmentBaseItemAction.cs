// <copyright file="IEnvironmentBaseItemAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Item Base Action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    public interface IEnvironmentBaseItemAction<TInput, TState, TResult> : IEntityAction<TInput, TState, TResult>
    {
    }
}
