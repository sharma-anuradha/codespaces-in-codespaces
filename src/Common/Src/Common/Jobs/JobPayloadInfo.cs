// <copyright file="JobPayloadInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// The payload info being used in the Queue.
    /// </summary>
    internal class JobPayloadInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobPayloadInfo"/> class.
        /// </summary>
        /// <param name="tagType">Tag type.</param>
        /// <param name="payload">Json payload content.</param>
        /// <param name="created">When this payload was created.</param>
        /// <param name="retries">Number of retries.</param>
        public JobPayloadInfo(string tagType, string payload, DateTime created, int retries = 0)
        {
            TagType = tagType;
            Payload = payload;
            Created = created;
            Retries = retries;
        }

        /// <summary>
        /// Gets the tag type.
        /// </summary>
        public string TagType { get; }

        /// <summary>
        /// Gets the payload json content.
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// Gets or sets the job payload options.
        /// </summary>
        public JobPayloadOptions PayloadOptions { get; set; }

        /// <summary>
        /// Gets the creation date/time.
        /// </summary>
        public DateTime Created { get; }

        /// <summary>
        /// Gets or sets the number of retries.
        /// </summary>
        public int Retries { get; set; }

        /// <summary>
        /// Deserialize an instance from json.
        /// </summary>
        /// <param name="json">The json content.</param>
        /// <returns>An instance of this type.</returns>
        public static JobPayloadInfo FromJson(string json)
        {
            return JsonConvert.DeserializeObject<JobPayloadInfo>(json);
        }

        /// <summary>
        /// Convert this instance to json content.
        /// </summary>
        /// <returns>json content.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
