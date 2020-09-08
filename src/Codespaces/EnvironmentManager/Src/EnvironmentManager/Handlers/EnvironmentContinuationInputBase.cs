// <copyright file="EnvironmentContinuationInputBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// The environment continutaion base class.
    /// </summary>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    public class EnvironmentContinuationInputBase<TState> : ContinuationJobPayload<TState>
       where TState : struct, System.Enum
    {
        /// <summary>
        /// Gets or sets the reference id.
        /// </summary>
        public Guid EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the triggered that caused the operation.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this env continutaion was initialized.
        /// </summary>
        public bool IsInitialized { get; set; }
    }
}
