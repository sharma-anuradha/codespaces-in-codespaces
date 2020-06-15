// <copyright file="ContinuationOperationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Represents the continuation input which has a target input.
    /// </summary>
    public class ContinuationOperationInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the reference id.
        /// </summary>
        public Guid EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the triggered that caused the operation.
        /// </summary>
        public string Reason { get; set; }
    }
}
