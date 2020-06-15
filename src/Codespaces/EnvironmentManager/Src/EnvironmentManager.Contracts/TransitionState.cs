// <copyright file="TransitionState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// State information for a given transition.
    /// </summary>
    public class TransitionState
    {
        /// <summary>
        /// Gets or sets the Attempt Count.
        /// </summary>
        [JsonProperty(PropertyName = "attemptCount")]
        public int AttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the reason.
        /// </summary>
        [JsonProperty(PropertyName = "reason")]
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationState? Status { get; set; }

        /// <summary>
        /// Gets or sets the status changed.
        /// </summary>
        [JsonProperty(PropertyName = "statusChanged")]
        public DateTime? StatusChanged { get; set; }

        /// <summary>
        /// Gets or sets the status changes.
        /// </summary>
        [JsonProperty(PropertyName = "statusChanges")]
        public IList<OperationStateChanges> StatusChanges { get; set; }

        /// <summary>
        /// Updates the status.
        /// </summary>
        /// <param name="newState">Target new state.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="newTime">Time if that is being set.</param>
        /// <returns>Returns if the update occured.</returns>
        public bool UpdateStatus(OperationState newState, string trigger, DateTime? newTime = null)
        {
            if (Status.HasValue && Status.Value == newState)
            {
                return false;
            }

            if (StatusChanges == null)
            {
                StatusChanges = new List<OperationStateChanges>();
            }

            var time = newTime.GetValueOrDefault(DateTime.UtcNow);
            Status = newState;
            StatusChanged = time;
            StatusChanges.Add(new OperationStateChanges
            {
                Status = newState,
                Time = time,
                Trigger = trigger,
            });

            return true;
        }

        /// <summary>
        /// Resets the status.
        /// </summary>
        /// <param name="resetAttemptCount">Target reset attempt count.</param>
        /// <returns>Whether ant change was needed.</returns>
        public bool ResetStatus(bool resetAttemptCount)
        {
            // Only update if we have to.
            if (Status != null)
            {
                Status = null;
                StatusChanged = null;
                StatusChanges = null;
                Reason = null;
                if (resetAttemptCount)
                {
                    AttemptCount = 0;
                }
                else
                {
                    AttemptCount++;
                }

                return true;
            }

            return false;
        }
    }
}
