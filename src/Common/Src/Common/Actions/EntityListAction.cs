// <copyright file="EntityListAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity list action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    /// <typeparam name="TResult">Working result type.</typeparam>
    /// <typeparam name="TRepository">Repository type.</typeparam>
    public abstract class EntityListAction<TInput, TState, TResult, TRepository> : EntityAction<TInput, TState, IEnumerable<TResult>, IEnumerable<TResult>, TRepository, TResult>
        where TResult : TaggedEntity
        where TRepository : class, IEntityRepository<TResult>
        where TState : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityListAction{TInput, TState, TResult, TRepository}"/> class.
        /// </summary>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        protected EntityListAction(
            TRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo)
            : base(repository, currentLocationProvider, currentUserProvider, controlPlaneInfo)
        {
        }

        /// <inheritdoc/>
        protected override string EntityName => "Environment";
    }
}
