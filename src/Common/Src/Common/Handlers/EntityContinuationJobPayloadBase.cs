// <copyright file="EntityContinuationJobPayloadBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers
{
    /// <summary>
    /// The environment continutaion base class.
    /// </summary>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    public class EntityContinuationJobPayloadBase<TState> : ContinuationJobPayload<TState>
       where TState : struct, System.Enum
    {
        /// <summary>
        /// Gets or sets the reference id.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Gets or sets the triggered that caused the operation.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this continutaion was initialized.
        /// </summary>
        public bool IsInitialized { get; set; }
    }
}
