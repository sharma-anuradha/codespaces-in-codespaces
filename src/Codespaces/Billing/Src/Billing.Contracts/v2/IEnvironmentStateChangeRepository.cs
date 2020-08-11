// <copyright file="IEnvironmentStateChangeRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository
{
    /// <summary>
    /// the interface for the billing summary repository.
    /// </summary>
    public interface IEnvironmentStateChangeRepository : IDocumentDbCollection<EnvironmentStateChange>
    {
        /// <summary>
        /// Gets all environment events for a particular planId within a given time window.
        /// </summary>
        /// <param name="planId">the planId we are trying to get events for.</param>
        /// <param name="startTime">the start time we want events after.</param>
        /// <param name="endTime">the end time we want events events preceeding.</param>
        /// <param name="logger">the logger.</param>
        /// <returns> a list of events between the time range specified for the planId.</returns>
        Task<IEnumerable<EnvironmentStateChange>> GetAllEnvironmentEventsAsync(string planId, DateTime startTime, DateTime endTime, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environment state changes.
        /// </summary>
        /// <param name="planId">the planId we are trying to get events for.</param>
        /// <param name="endTime">the end time we want events events preceeding.</param>
        /// <param name="logger">the logger.</param>
        /// <returns> a list of events between the time range specified for the planId.</returns>
        Task<IEnumerable<EnvironmentStateChange>> GetAllEnvironmentEventsAsync(string planId, DateTime endTime, IDiagnosticsLogger logger);
    }
}
