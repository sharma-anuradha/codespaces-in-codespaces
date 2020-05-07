// <copyright file="QueueMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Sends a queue message to vmagent.
    /// </summary>
    [DataContract]
    public class QueueMessage
    {
        /// <summary>
        /// Gets or sets the job command name.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the job id.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the job arguments.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Dictionary<string, string> Parameters { get; set; }
    }
}