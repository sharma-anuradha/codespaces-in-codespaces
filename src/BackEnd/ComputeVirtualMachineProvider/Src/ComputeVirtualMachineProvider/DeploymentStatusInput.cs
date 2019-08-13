// <copyright file="DeploymentStatusInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class DeploymentStatusInput
    {
        [JsonConstructor]
        public DeploymentStatusInput(string trackingId, ResourceId resourceId)
        {
            TrackingId = trackingId;
            ResourceId = resourceId;
        }

        public string TrackingId { get; }

        public ResourceId ResourceId { get; }
    }
}