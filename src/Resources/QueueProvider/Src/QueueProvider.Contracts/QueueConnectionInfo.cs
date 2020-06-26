// <copyright file="QueueConnectionInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts
{
    /// <summary>
    /// Queue connection info. Url and the sas token for access.
    /// </summary>
    public class QueueConnectionInfo
    {
        /// <summary>
        /// Gets or sets name of the queue.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Aure queue url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets azure queue sasToken.
        /// </summary>
        public string SasToken { get; set; }
    }
}
