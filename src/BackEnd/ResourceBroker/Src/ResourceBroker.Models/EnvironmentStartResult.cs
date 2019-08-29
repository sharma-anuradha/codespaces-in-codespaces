﻿// <copyright file="EnvironmentStartResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Model required for Deallocation result.
    /// </summary>
    public class EnvironmentStartResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        public bool Successful { get; set; }
    }
}
