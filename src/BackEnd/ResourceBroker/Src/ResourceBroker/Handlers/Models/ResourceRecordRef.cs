// <copyright file="ContinuationTaskActivatorExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    public class ResourceRecordRef
    {
        public ResourceRecordRef(ResourceRecord value)
        {
            Value = value;
        }

        public ResourceRecord Value { get; set; }
    }
}
