// <copyright file="EnvironmentStateTransitionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment State Transition Input.
    /// </summary>
    public class EnvironmentStateTransitionInput : EnvironmentMonitorContinuationInput
    {
        /// <summary>
        /// Gets or sets current environment state.
        /// </summary>
        public CloudEnvironmentState CurrentState { get; set; }

        /// <summary>
        /// Gets or sets target environment state.
        /// </summary>
        public CloudEnvironmentState TargetState { get; set; }

        /// <summary>
        /// Gets or sets environemnt state tranisition timeout.
        /// </summary>
        public TimeSpan TransitionTimeout { get; set; }
    }
}