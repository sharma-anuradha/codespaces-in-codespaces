// <copyright file="ContinuationTaskActivatorExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    public class ResourceReference
    {
        public ResourceReference(IResourceRepository resourceRepository)
        {
            ResourceRepository = resourceRepository;
        }

        public ResourceRecord Resource { get; private set; }

        private IResourceRepository ResourceRepository { get; }

        private Func<ResourceRecord, OperationState, bool> UpdateStartingStatusFunc { get; } = (r, s) => r.UpdateStartingStatus(s);

        private Func<ResourceRecord, OperationState, bool> UpdateDeletingStatusFunc { get; } = (r, s) => r.UpdateDeletingStatus(s);

        private Func<ResourceRecord, OperationState, bool> UpdateProvisioningStatusFunc { get; } = (r, s) => r.UpdateProvisioningStatus(s);

        public async Task PopulateAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            Resource = await ResourceRepository.GetAsync(resourceId.ToString(), logger.FromExisting());
            if (Resource == null)
            {
                throw new ResourceNotFoundException(resourceId);
            }
        }

        public async Task SaveStartingStatus(OperationState state, IDiagnosticsLogger logger)
        {
            await SaveStatus(UpdateStartingStatusFunc, state, logger);
        }

        public async Task SaveDeletingStatus(OperationState state, IDiagnosticsLogger logger)
        {
            await SaveStatus(UpdateDeletingStatusFunc, state, logger);
        }

        public async Task SaveProvisioningStatus(OperationState state, IDiagnosticsLogger logger)
        {
            await SaveStatus(UpdateProvisioningStatusFunc, state, logger);
        }

        private async Task SaveStatus(Func<ResourceRecord, OperationState, bool> updateStatus, OperationState state, IDiagnosticsLogger logger)
        {
            if (updateStatus(Resource, state))
            {
                Resource = await ResourceRepository.UpdateAsync(Resource, logger.FromExisting());
            }
        }
    }
}
