// <copyright file="ContinuationOperationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Represents the continuation input which has a target input.
    /// </summary>
    public class ContinuationOperationInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the reference id.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the target input.
        /// </summary>
        public ContinuationInput OperationInput { get; set; }
    }
}
