// <copyright file="CommonMessageCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// List general error codes returned by APIs.
    /// Reserving error codes 500-599 for common errors.
    /// </summary>
    public enum CommonMessageCodes : int
    {
        /// <summary>
        /// Record is modified externally.
        /// </summary>
        ConcurrentModification = 501,
    }
}
