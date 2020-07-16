// <copyright file="IEntityActionIdInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// Entity Action Id Input.
    /// </summary>
    public interface IEntityActionIdInput
    {
        /// <summary>
        /// Gets the id for this input.
        /// </summary>
        Guid Id { get; }
    }
}
