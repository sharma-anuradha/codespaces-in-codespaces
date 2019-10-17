using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Lists of codes returned from <see cref="IPlanManager"/>.
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Quota exceeded.
        /// </summary>
        ExceededQuota = 1,

        /// <summary>
        /// VsoPlan does not exist.
        /// </summary>
        PlanDoesNotExist = 2,
    }
}
