// <copyright file="CallbackInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The environment registration callback input.
    /// </summary>
    public class CallbackOptionsBody
    {
        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the environment registration payload.
        /// </summary>
        public CallbackPayloadOptionsBody Payload { get; set; }
    }
}
