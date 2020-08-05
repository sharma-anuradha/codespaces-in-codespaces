// <copyright file="BaseEntityTransition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity Transition.
    /// </summary>
    /// <typeparam name="T">Type of entity being targeted.</typeparam>
    public abstract class BaseEntityTransition<T> : IEntityTransition<T>
        where T : TaggedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEntityTransition{T}"/> class.
        /// </summary>
        /// <param name="value">Target entity value.</param>
        protected BaseEntityTransition(T value)
        {
            Value = value;
            Transitions = new List<Func<T, Task>>();
        }

        /// <inheritdoc/>
        public T Value { get; private set; }

        /// <inheritdoc/>
        public bool IsDirty
        {
            get { return Transitions.Any(); }
        }

        private IList<Func<T, Task>> Transitions { get; set; }

        /// <inheritdoc/>
        public abstract bool IsValid(T persistedEntity);

        /// <inheritdoc/>
        public async Task PushTransitionAsync(Func<T, Task> transition)
        {
            // Add transition to the ordered play back list.
            Transitions.Add(transition);

            // Apply transition now so that down stream code will recieve the mutations.
            await transition(Value);
        }

        /// <inheritdoc/>
        public void PushTransition(Action<T> transition)
        {
            // Add transition to the ordered play back list.
            Transitions.Add((value) =>
            {
                transition(value);
                return Task.CompletedTask;
            });

            // Apply transition now so that down stream code will recieve the mutations.
            transition(Value);
        }

        /// <inheritdoc/>
        public async Task ReplaceAndReplayTransitionsAsync(T value)
        {
            // Repalce value
            Value = value;

            // Reapply transitions
            foreach (var transition in Transitions)
            {
                await transition(Value);
            }
        }

        /// <inheritdoc/>
        public void ReplaceAndResetTransition(T value)
        {
            // Repalce value
            Value = value;

            // Clear transitions
            Transitions.Clear();
        }
    }
}
