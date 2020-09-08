// <copyright file="IArchiveEnvironmentContinuationPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// The archive continuation payload definition.
    /// </summary>
    public interface IArchiveEnvironmentContinuationPayload : IEnvironmentContinuationPayload
    {
        /// <summary>
        /// Gets or sets the Archive State.
        /// </summary>
        public ArchiveEnvironmentContinuationInputState ArchiveStatus { get; set; }

        /// <summary>
        /// Gets or sets the Archiuve Resource that exists in the continuation.
        /// </summary>
        public EnvironmentContinuationInputResource ArchiveResource { get; set; }

        /// <summary>
        /// Gets or sets the last state updated time.
        /// </summary>
        public DateTime LastStateUpdated { get; set; }
    }
}
