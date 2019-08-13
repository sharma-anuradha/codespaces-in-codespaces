using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Enum which represents the different states the resource can go
    /// through when being provisioned.
    /// </summary>
    public enum ResourceProvisioningStatus
    {
        /// <summary>
        /// 
        /// </summary>
        Queued,

        /// <summary>
        /// 
        /// </summary>
        Provisioning,

        /// <summary>
        /// 
        /// </summary>
        Completed
    }
}
