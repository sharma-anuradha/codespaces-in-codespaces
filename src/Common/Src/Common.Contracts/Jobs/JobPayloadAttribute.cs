// <copyright file="JobPayloadAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// The job payload options
    /// </summary>
    public enum JobPayloadNameOption
    {
        None,

        Name,

        FullName,
    }

    /// <summary>
    /// Attribute to apply on a payload type class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class JobPayloadAttribute : Attribute
    {
        public JobPayloadAttribute(string typeName)
        {
            Requires.NotNullOrEmpty(typeName, nameof(typeName));
            NameOption = JobPayloadNameOption.None;
            TypeName = typeName;
        }

        public JobPayloadAttribute(JobPayloadNameOption nameOption)
        {
            NameOption = nameOption;
        }

        /// <summary>
        /// Gets the name option
        /// </summary>
        public JobPayloadNameOption NameOption { get; }

        /// <summary>
        /// Gets the type name
        /// </summary>
        public string TypeName { get; }
    }
}
