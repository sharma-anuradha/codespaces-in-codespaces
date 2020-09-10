// <copyright file="CloudEnvironmentParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Parameters required to start the compute of a CloudEnvironment.
    /// </summary>
    public abstract class CloudEnvironmentParameters
    {
        /// <summary>
        /// Gets or sets the user claims to store in the environment connection token.
        /// </summary>
        public Profile UserProfile { get; set; }

        /// <summary>
        /// Gets or sets the callback uri format.
        /// </summary>
        public string CallbackUriFormat { get; set; }

        /// <summary>
        /// Gets or sets the service uri.
        /// </summary>
        public Uri FrontEndServiceUri { get; set; }

        /// <summary>
        /// Gets or sets the user token.
        /// </summary>
        public string UserAuthToken { get; set; }

        /// <summary>
        /// Gets or sets the user id set.
        /// </summary>
        public UserIdSet CurrentUserIdSet { get; set; }

        /// <summary>
        /// Gets or sets the secrets sent from Create/Resume request.
        /// </summary>
        public IEnumerable<SecretDataBody> Secrets { get; set; }
    }
}
