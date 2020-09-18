// <copyright file="ResourceStateManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Updates Resource state.
    /// </summary>
    public class ResourceStateManager : IResourceStateManager
    {
        private const string LogBaseName = "resource_state_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceStateManager"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository.</param>
        /// <param name="requestQueueManager">Resource request manager.</param>
        public ResourceStateManager(
            IResourceRepository resourceRepository,
            IResourceRequestManager requestQueueManager)
        {
            ResourceRepository = resourceRepository;
            ResourceRequestManager = requestQueueManager;
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceRequestManager ResourceRequestManager { get; }

        /// <inheritdoc/>
        public Task<ResourceRecord> MarkResourceReady(ResourceRecord resource, string reason, IDiagnosticsLogger logger)
        {
            var updatedResource = resource;
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_mark_resource_ready",
                async (IDiagnosticsLogger innerLogger) =>
                {
                    if (!resource.IsAssigned)
                    {
                        resource = await ResourceRequestManager.TryAssignAsync(resource, reason, logger);
                    }

                    // Update os disk record if it exists.
                    if (resource.Type == Common.Contracts.ResourceType.ComputeVM)
                    {
                        var computeDetails = resource.GetComputeDetails();
                        var osDiskRecordId = computeDetails.OSDiskRecordId;
                        if (osDiskRecordId != default)
                        {
                            var osDiskResourceRecord = await ResourceRepository.GetAsync(osDiskRecordId.ToString(), logger.NewChildLogger());
                            if (osDiskResourceRecord != default)
                            {
                                if (!osDiskResourceRecord.IsReady)
                                {
                                    osDiskResourceRecord.IsReady = true;
                                    osDiskResourceRecord.Ready = DateTime.UtcNow;
                                }

                                // Copies over heartbeat information to OSDisk as well. When compute is gone, we will rely on the information in the OSDisk.
                                osDiskResourceRecord.HeartBeatSummary = resource.HeartBeatSummary;

                                await ResourceRepository.UpdateAsync(osDiskResourceRecord, logger.NewChildLogger());
                            }
                        }
                    }

                    // Update core properties to indicate that its assigned
                    resource.IsReady = true;
                    resource.Ready = DateTime.UtcNow;

                    updatedResource = await ResourceRepository.UpdateAsync(resource, logger.NewChildLogger());
                    return updatedResource;
                });
        }
    }
}