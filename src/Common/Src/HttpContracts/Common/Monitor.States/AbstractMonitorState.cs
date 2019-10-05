// <copyright file="AbstractMonitorState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Abstract state representing monitor data.
    /// </summary>
    public abstract class AbstractMonitorState
    {
        /// <summary>
        /// Gets or sets the UTC timestamp at which the data is collected.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or Sets the Name.
        /// </summary>
        public string Name { get; set; }
    }
}
