// <copyright file="ResourceRecordRef.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        /// <param name="resourceId">Target resource id.</param>
        public ResourceRecordRef(ResourceRecord value, Guid resourceId)
            : this(resourceId)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRecordRef"/> class.
        /// </summary>
        /// <param name="resourceId">Target resource id.</param>
        public ResourceRecordRef(Guid resourceId)
        {
            ResourceId = resourceId;
        }

        /// <summary>
        /// Gets or sets target value.
        /// </summary>
        public ResourceRecord Value { get; set; }

        /// <summary>
        /// Gets or sets the resource id.
        /// </summary>
        public Guid ResourceId { get; set; }
    }
}
