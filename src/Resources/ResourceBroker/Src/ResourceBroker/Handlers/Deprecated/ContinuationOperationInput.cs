// <copyright file="ContinuationOperationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
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
        [JsonConverter(typeof(ContinuationInputConverter))]
        public ContinuationInput OperationInput { get; set; }

        /// <summary>
        /// Gets or sets the triggered that caused the operation.
        /// </summary>
        public string Reason { get; set; }
    }
}
