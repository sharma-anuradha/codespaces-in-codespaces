// <copyright file="GitChangesData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents the state of Git Changes.
    /// </summary>
    [DataContract]
    public class GitChangesData : CollectedData
    {
        /// <summary>
        /// Gets or sets a value indicating whether git has changes.
        /// </summary>
        [DataMember]
        public bool HasChanges { get; set; }
    }
}
