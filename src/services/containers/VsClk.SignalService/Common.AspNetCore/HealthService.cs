// <copyright file="HealthService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Managed the overall state of the app health
    /// </summary>
    public class HealthService
    {
        private readonly IList<IHealthStatusProvider> healthStatusProviders;

        public HealthService(
            IList<IHealthStatusProvider> healthStatusProviders)
        {
            this.healthStatusProviders = healthStatusProviders;
        }

        /// <summary>
        /// Return the overall health status of the app
        /// </summary>
        public bool State
        {
            get
            {
                return this.healthStatusProviders.FirstOrDefault(ws => !ws.IsHealthy) == null;
            }
        }

        /// <summary>
        ///  return each provider health state
        /// </summary>
        /// <returns></returns>
        public (Type, object)[] GetProvidersStatus()
        {
            return this.healthStatusProviders.Select(p => (p.GetType(), p.Status)).ToArray();
        }
    }
}
