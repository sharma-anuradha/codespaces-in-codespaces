﻿using System;
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
                return this.healthStatusProviders.FirstOrDefault(ws => !ws.State) == null;
            }
        }

        /// <summary>
        ///  return each provider health state
        /// </summary>
        /// <returns></returns>
        public (Type, bool)[] GetProvidersStatus()
        {
            return this.healthStatusProviders.Select(p => (p.GetType(), p.State)).ToArray();
        }
    }
}
