// <copyright file="ICloudEnvironmentHeartbeatRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A repository for CloudEnvironmentHeartbeat/>.
    /// </summary>
    public interface ICloudEnvironmentHeartbeatRepository : IDocumentDbCollection<CloudEnvironmentHeartbeat>
    {
    }
}
