﻿// <copyright file="QueueConnectionInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// Provides azure queue sas token and url.
    /// </summary>
    public class QueueConnectionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConnectionInfo"/> class.
        /// </summary>
        public QueueConnectionInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConnectionInfo"/> class.
        /// </summary>
        /// <param name="url">Azure queue url.</param>
        /// <param name="sasToken">Azure queue sas token.</param>
        public QueueConnectionInfo(string url, string sasToken)
        {
            this.Url = url;
            this.SasToken = sasToken;
        }

        /// <summary>
        /// Gets Aure queue url.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Gets aure queue sasToken.
        /// </summary>
        public string SasToken { get; }
    }
}