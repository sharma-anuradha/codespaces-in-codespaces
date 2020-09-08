// <copyright file="IEnvironmentContinuationPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// The environment continutaion payload.
    /// </summary>
    public interface IEnvironmentContinuationPayload
    {
        /// <summary>
        /// Gets the reference id.
        /// </summary>
        public Guid EnvironmentId { get; }
    }
}
