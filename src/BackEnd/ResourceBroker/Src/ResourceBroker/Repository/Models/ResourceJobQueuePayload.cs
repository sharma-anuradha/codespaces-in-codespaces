// <copyright file="ResourceJobQueuePayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    public class ResourceJobQueuePayload
    {
        public string Target { get; set; }

        public string TrackingId { get; set; }

        public DateTime Created { get; set; }

        public object Input { get; set; }

        public object Metadata { get; set; }

        public string ContinuationToken { get; set; }

        public OperationState? Status { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}