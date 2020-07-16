// <copyright file="EntityListAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity list action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TResult">Working result type.</typeparam>
    /// <typeparam name="TRepository">Repository type.</typeparam>
    public abstract class EntityListAction<TInput, TResult, TRepository> : EntityAction<TInput, IEnumerable<TResult>, IEnumerable<TResult>, TRepository, TResult>
        where TResult : TaggedEntity
        where TRepository : class, IDocumentDbCollection<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityListAction{TInput, TResult, TRepository}"/> class.
        /// </summary>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="systemActionGetProvider">Target system action get provider.</param>
        public EntityListAction(
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
