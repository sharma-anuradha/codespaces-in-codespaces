// <copyright file="CollectedDataHandlerContext.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring
{
    /// <summary>
    /// The class will store the final state after processing all heart beat collected data result.
    /// </summary>
    public class CollectedDataHandlerContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectedDataHandlerContext"/> class.
        /// </summary>
        /// <param name="cloudEnvironment"><see cref="CloudEnvironmentTransition"/>.</param>
        public CollectedDataHandlerContext(EnvironmentTransition cloudEnvironment)
        {
            CloudEnvironmentTransition = cloudEnvironment;
        }

        /// <summary>
        /// Gets or sets the cloud environment object.
        /// </summary>
        public EnvironmentTransition CloudEnvironmentTransition { get; set; }

        /// <summary>
        /// Gets or sets the new state of the cloud environment.
        /// </summary>
        public CloudEnvironmentState CloudEnvironmentState { get; set; }

        /// <summary>
        /// Gets or sets the reason for the environment update.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the trigger for the environment update.
        /// </summary>
        public string Trigger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the failure is because of user error.
        /// </summary>
        public bool? IsUserError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the failure is because of user error.
        /// </summary>
        public bool StopProcessing { get; set; }
    }
}