// <copyright file="IEntityAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity Action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    public interface IEntityAction<TInput, TResult>
    {
        /// <summary>
        /// Run action.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from action.</returns>
        Task<TResult> Run(TInput input, IDiagnosticsLogger logger);
    }
}
