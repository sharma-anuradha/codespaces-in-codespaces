// <copyright file="ErrorMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Port forwarding agent error message.
    /// </summary>
    public class ErrorMessage
    {
        /// <summary>
        /// Gets or sets error message.
        /// </summary>
        public string Message { get; set; } = default!;

        /// <summary>
        /// Gets or sets error details.
        /// </summary>
        public string Detail { get; set; } = default!;
    }
}
