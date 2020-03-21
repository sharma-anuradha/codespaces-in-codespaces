// <copyright file="EnvironmentRecordRef.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Resource Record Ref.
    /// </summary>
    public class EnvironmentRecordRef
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentRecordRef"/> class.
        /// </summary>
        /// <param name="value">Target value.</param>
        public EnvironmentRecordRef(CloudEnvironment value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets target value.
        /// </summary>
        public CloudEnvironment Value { get; set; }
    }
}
