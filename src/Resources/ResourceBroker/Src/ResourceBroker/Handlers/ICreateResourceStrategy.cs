// <copyright file="ICreateResourceStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Resource Creation Strategis.
    /// </summary>
    public interface ICreateResourceStrategy
    {
        /// <summary>
        /// Returns true if it can create the resource.
        /// </summary>
        /// <param name="input">input.</param>
        /// <returns>result.</returns>
        bool CanHandle(CreateResourceContinuationInput input);

        /// <summary>
        /// Build create resource operation input.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="resource">resource record.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationInput> BuildCreateOperationInputAsync(
            CreateResourceContinuationInput input,
            ResourceRecordRef resource,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Run create resource operation.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="resource">resource record.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(
            CreateResourceContinuationInput input,
            ResourceRecordRef resource,
            IDiagnosticsLogger logger);
    }
}