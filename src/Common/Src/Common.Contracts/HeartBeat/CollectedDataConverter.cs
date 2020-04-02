// <copyright file="CollectedDataConverter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Converting JSON representation of <see cref="CollectedData"/> to concrete object.
    /// </summary>
    public class CollectedDataConverter : JsonConverter<CollectedData>
    {
        /// <inheritdoc/>
        public override CollectedData ReadJson(JsonReader reader, Type objectType, CollectedData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                var collectedData = JObject.Load(reader);
                var name = (string)collectedData["name"];
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                if (name.Equals(nameof(EnvironmentData), StringComparison.OrdinalIgnoreCase))
                {
                    return collectedData.ToObject<EnvironmentData>();
                }
                else if (!string.IsNullOrEmpty((string)collectedData["jobState"]))
                {
                    return collectedData.ToObject<JobResult>();
                }
                else if (name.Equals(nameof(EnvironmentSessionData), StringComparison.OrdinalIgnoreCase))
                {
                    return collectedData.ToObject<EnvironmentSessionData>();
                }
                else if (name.Equals(nameof(GitChangesData), StringComparison.OrdinalIgnoreCase))
                {
                    return collectedData.ToObject<GitChangesData>();
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, CollectedData value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
