// <copyright file="EntityAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <typeparam name="TModel">Working model type.</typeparam>
    /// <typeparam name="TRepository">Repository type.</typeparam>
    /// <typeparam name="TRepositoryModel">Repository model type.</typeparam>
    public abstract class EntityAction<TInput, TResult, TModel, TRepository, TRepositoryModel> : IEntityAction<TInput, TResult>
        where TRepository : class, IDocumentDbCollection<TRepositoryModel>
        where TRepositoryModel : TaggedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityAction{TInput, TResult, TModel, TRepository, TRepositoryModel}"/> class.
        /// </summary>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="systemActionGetProvider">Target system action get provider.</param>
        public EntityAction(
            TRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo)
        {
            Repository = Requires.NotNull(repository, nameof(repository));
            CurrentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        /// <summary>
        /// Gets target repository.
        /// </summary>
        protected TRepository Repository { get; }

        /// <summary>
        /// Gets target Current Location Provider.
        /// </summary>
        protected ICurrentLocationProvider CurrentLocationProvider { get; }

        /// <summary>
        /// Gets target Current User Provider.
        /// </summary>
        protected ICurrentUserProvider CurrentUserProvider { get; }

        /// <summary>
        /// Gets target Control Plane Info.
        /// </summary>
        protected IControlPlaneInfo ControlPlaneInfo { get; }

        /// <summary>
        /// Gets the base logging name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets the entity name.
        /// </summary>
        protected abstract string EntityName { get; }

        /// <summary>
        /// Run action.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the action.</returns>
        public virtual async Task<TResult> Run(TInput input, IDiagnosticsLogger logger)
        {
            var result = default(TResult);

            await logger.OperationScopeWithThrowingCustomExceptionControlFlowAsync(
                LogBaseName,
                async (childLogger) =>
                {
                    result = await RunCoreAsync(input, childLogger);
                },
                async (ex, childLogger) =>
                {
                    return await HandleExceptionAsync(input, ex, childLogger);
                });

            return result;
        }

        /// <summary>
        /// Process input post logging.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resulting object of action.</returns>
        protected abstract Task<TResult> RunCoreAsync(TInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Handle exceptions from RunCoreAsync.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="ex">Target Exception.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns true if the exception is fully handled. false otherwise.</returns>
        protected virtual Task<bool> HandleExceptionAsync(TInput input, Exception ex, IDiagnosticsLogger logger) => Task.FromResult(false);

        /// <summary>
        /// Check Location Validity.
        /// </summary>
        /// <param name="resourceLocation">Target resource location.</param>
        /// <param name="logger">Target logger.</param>
        protected void ValidateTargetLocation(AzureLocation resourceLocation, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("ResourceLocation", resourceLocation.ToString());

            var resourceOwningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(resourceLocation);

            logger.FluentAddValue("ResourceOwningStampLocation", resourceOwningStamp.Location.ToString())
                .FluentAddValue("CurrentStampLocation", CurrentLocationProvider.CurrentLocation.ToString());

            if (resourceOwningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                throw new RedirectToLocationException("Invalid location.", resourceOwningStamp.DnsHostName);
            }
        }
    }
}
