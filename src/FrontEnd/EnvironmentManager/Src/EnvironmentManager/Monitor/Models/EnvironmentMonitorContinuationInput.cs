﻿// <copyright file="EnvironmentMonitorContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models
{
    /// <summary>
    /// Base class for Environment Monitor Input.
    /// </summary>
    public class EnvironmentMonitorContinuationInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the reference id.
        /// </summary>
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the compute resource id.
        /// </summary>
        public Guid ComputeResourceId { get; set; }
    }
}