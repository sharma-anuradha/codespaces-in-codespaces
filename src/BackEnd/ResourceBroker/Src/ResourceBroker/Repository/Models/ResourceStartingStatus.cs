using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Enum which represents the different states the resource can go
    /// through when being started.
    /// </summary>
    public enum ResourceStartingStatus
    {
        /// <summary>
        /// 
        /// </summary>
        Initialized,

        /// <summary>
        /// 
        /// </summary>
        Waiting,

        /// <summary>
        /// 
        /// </summary>
        Complete
    }
}
