// <copyright file="EntityItemAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity item action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <typeparam name="TEntityTransition">Working model type.</typeparam>
    /// <typeparam name="TRepository">Repository type.</typeparam>
    /// <typeparam name="TRepositoryModel">Repository model type.</typeparam>
    public abstract class EntityItemAction<TInput, TState, TResult, TEntityTransition, TRepository, TRepositoryModel> : EntityAction<TInput, TState, TResult, TEntityTransition, TRepository, TRepositoryModel>
        where TRepositoryModel : TaggedEntity
        where TEntityTransition : class, IEntityTransition<TRepositoryModel>
        where TRepository : class, IEntityRepository<TRepositoryModel>
        where TState : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityItemAction{TInput, TState, TResult, TEntityTransition, TRepository, TRepositoryModel}"/> class.
        /// </summary>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        protected EntityItemAction(
            TRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo)
            : base(repository, currentLocationProvider, currentUserProvider, controlPlaneInfo)
        {
        }

        /// <summary>
        /// Builds model to use.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns working model or null if not found.</returns>
        protected virtual async Task<TEntityTransition> FetchOrGetDefaultAsync(TInput input, IDiagnosticsLogger logger)
        {
            var id = default(Guid);

            // Fetch id if we can
            if (input is Guid)
            {
                id = (Guid)(object)input;
            }
            else if (input is IEntityActionIdInput typedInput)
            {
                id = typedInput.Id;
            }
            else
            {
                throw new NotSupportedException($"Input type of '{input.GetType()}' is not supported by this default `FetchAsync` implementation.");
            }

            // Fetch environment transition
            return await Repository.BuildTransitionAsync(
                EntityName, id.ToString(), BuildTransition, logger.NewChildLogger());
        }

        /// <summary>
        /// Builds model to use.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns working model. Throws <see cref="EntityNotFoundException"/> if record does not exist.</returns>
        protected virtual async Task<TEntityTransition> FetchAsync(TInput input, IDiagnosticsLogger logger)
        {
            var record = await FetchOrGetDefaultAsync(input, logger);
            if (record == null)
            {
                throw new EntityNotFoundException($"Target not found.");
            }

            return record;
        }

        /// <summary>
        /// Runs core operation.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Target record.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resulting object of action.</returns>
        protected virtual async Task UpdateAsync(TInput input, TEntityTransition record, IDiagnosticsLogger logger)
        {
            // Push Updates
            await Repository.UpdateTransitionAsync(record.GetType().Name, record, logger.NewChildLogger());
        }

        /// <summary>
        /// Build Transition.
        /// </summary>
        /// <param name="model">Target model.</param>
        /// <returns>Returns built transition.</returns>
        protected abstract TEntityTransition BuildTransition(TRepositoryModel model);
    }
}
