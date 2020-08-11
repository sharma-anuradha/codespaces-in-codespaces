// <copyright file="EntityTransitionDocumentDbCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Document Db Collection Extensions.
    /// </summary>
    public static class EntityTransitionDocumentDbCollectionExtensions
    {
        /// <summary>
        /// Build transition for an entity.
        /// </summary>
        /// <param name="documentDbCollection">Target document db colleciton.</param>
        /// <param name="name">Target name.</param>
        /// <param name="key">Target key.</param>
        /// <param name="factory">Target factory.</param>
        /// <param name="logger">Target logger.</param>
        /// <typeparam name="TEntity">Type of entity being targeted.</typeparam>
        /// <typeparam name="TEntityTransition">Type of entity transition being targeted.</typeparam>
        /// <returns>Populated entity transition object.</returns>
        public static Task<TEntityTransition> BuildTransitionAsync<TEntity, TEntityTransition>(
            this IDocumentDbCollection<TEntity> documentDbCollection,
            string name,
            DocumentDbKey key,
            Func<TEntity, TEntityTransition> factory,
            IDiagnosticsLogger logger)
                where TEntityTransition : class, IEntityTransition<TEntity>
                where TEntity : TaggedEntity
        {
            return logger.OperationScopeAsync(
                $"docdb_{name}_build_entity_transition",
                async (childLogger) =>
                {
                    var record = await documentDbCollection.GetAsync(key, logger.NewChildLogger());
                    if (record == null)
                    {
                        return null;
                    }

                    return factory(record);
                });
        }

        /// <summary>
        /// Apply entity transition to the database document.
        /// </summary>
        /// <param name="documentDbCollection">Target document db colleciton.</param>
        /// <param name="name">Target name.</param>
        /// <param name="entityTransition">Target entity transition.</param>
        /// <param name="logger">Target logger.</param>
        /// <typeparam name="TEntity">Type of entity being targeted.</typeparam>
        /// <returns>Populated entity transition object.</returns>
        public static Task UpdateTransitionAsync<TEntity>(
            this IDocumentDbCollection<TEntity> documentDbCollection,
            string name,
            IEntityTransition<TEntity> entityTransition,
            IDiagnosticsLogger logger)
                where TEntity : TaggedEntity
        {
            var initialRun = true;

            return logger.RetryOperationScopeAsync(
                $"docdb_{name}_apply_entity_transition",
                async (childLogger) =>
                {
                    var newEntity = default(TEntity);

                    childLogger.FluentAddValue("TransitionIsDirty", entityTransition.IsDirty);

                    // Bail if we don't need to do anything
                    if (!entityTransition.IsDirty)
                    {
                        return;
                    }

                    // If we have failed before, replay things
                    if (!initialRun)
                    {
                        // Fetch latest record
                        newEntity = await documentDbCollection.GetAsync(entityTransition.Value.Id, logger.NewChildLogger());

                        // Throw if entity transition is no longer valid.
                        if (!entityTransition.IsValid(newEntity))
                        {
                            throw new ConflictException((int)CommonMessageCodes.ConcurrentModification);
                        }

                        // Replay transitions
                        await entityTransition.ReplaceAndReplayTransitionsAsync(newEntity);
                    }

                    // Set before anything can throw
                    initialRun = false;

                    // Try updating again
                    newEntity = await documentDbCollection.UpdateAsync(entityTransition.Value, logger.NewChildLogger());

                    // Replace transition value
                    entityTransition.ReplaceAndResetTransition(newEntity);
                });
        }
    }
}
