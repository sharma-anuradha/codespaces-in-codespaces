// <copyright file="ResourceRecordRef.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Resource Record Ref.
    /// </summary>
    public class ResourceRecordRef
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRecordRef"/> class.
        /// </summary>
        /// <param name="value">Target value.</param>
        public ResourceRecordRef(ResourceRecord value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets target value.
        /// </summary>
        public ResourceRecord Value { get; set; }
    }
}
