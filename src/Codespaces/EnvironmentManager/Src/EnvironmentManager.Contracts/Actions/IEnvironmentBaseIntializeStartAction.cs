// <copyright file="IEnvironmentBaseIntializeStartAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Intialize Start Action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    public interface IEnvironmentBaseIntializeStartAction<TInput> : IEnvironmentItemAction<TInput, object>
    {
    }
}
