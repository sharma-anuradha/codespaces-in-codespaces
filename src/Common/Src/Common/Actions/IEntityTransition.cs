// <copyright file="IEntityTransition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity Transition.
    /// </summary>
    /// <typeparam name="T">Type of entity being targeted.</typeparam>
    public interface IEntityTransition<T>
        where T : TaggedEntity
    {
        /// <summary>
        /// Gets current record reference.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets a value indicating whether entity transition is dirty.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Compare with the entity in database to determine whether it is
        /// still in a valid state to apply transitions.
        /// </summary>
        /// <param name="persistedEntity">Entity in the database to compare against.</param>
        /// <returns>True if the transition is valid, false otherwise.</returns>
        bool IsValid(T persistedEntity);

        /// <summary>
        /// Adds transition to the list of transition actions and applies it
        /// straight away.
        /// </summary>
        /// <param name="transition">Target transition.</param>
        void PushTransition(Action<T> transition);

        /// <summary>
        /// Adds transition to the list of transition actions and applies it
        /// straight away.
        /// </summary>
        /// <param name="transition">Target transition.</param>
        /// <returns>Active task.</returns>
        Task PushTransitionAsync(Func<T, Task> transition);

        /// <summary>
        /// Replays applied transition in the order they were originally applied.
        /// </summary>
        /// <param name="value">Target value.</param>
        /// <returns>Active task.</returns>
        Task ReplaceAndReplayTransitionsAsync(T value);

        /// <summary>
        /// Replaces current value in transition value.
        /// </summary>
        /// <param name="value">Target value.</param>
        void ReplaceAndResetTransition(T value);
    }
}
