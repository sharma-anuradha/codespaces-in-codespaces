// <copyright file="ResourceComponentDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Resource Component Details.
    /// </summary>
    public class ResourceComponentDetail : IEquatable<ResourceComponentDetail>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceComponentDetail"/> class.
        /// </summary>
        public ResourceComponentDetail()
        {
            GraphVersion = 0;
        }

        /// <summary>
        /// Gets or sets the components for resource.
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public Dictionary<string, ResourceComponent> Items { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [JsonProperty(PropertyName = "graphVersion")]
        public int GraphVersion { get; set; }

        /// <inheritdoc/>
        public bool Equals(ResourceComponentDetail other)
        {
            if (other is null)
            {
                return false;
            }

            if (other.GraphVersion != GraphVersion)
            {
                return false;
            }

            if (other.Items?.Count != Items?.Count)
            {
                return false;
            }

            if (Items?.Count > 0
                && !Items.All(kvp =>
                    {
                        return other.Items.TryGetValue(kvp.Key, out var value2)
                            && (kvp.Value == null ? value2 == null : kvp.Value.Equals(value2));
                    }))
            {
                return false;
            }

            return true;
        }
    }
}
