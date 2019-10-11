// <copyright file="CollectedDataConverter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Converting JSON representation of AbstractMonitorState to concrete object.
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

                if (name.Equals((nameof(EnvironmentData), StringComparison.OrdinalIgnoreCase)))
                {
                    var environmentData = new EnvironmentData
                    {
                        EnvironmentId = (string)collectedData["environmentId"],
                        Name = (string)collectedData["name"],
                        SessionPath = (string)collectedData["sessionPath"],
                        State = collectedData["state"].ToObject<VsoEnvironmentState>(),
                        EnvironmentType = collectedData["environmentType"].ToObject<VsoEnvironmentType>(),
                        TimeStamp = (DateTime)collectedData["timestamp"],
                    };
                    return environmentData;
                }
                else if (Enum.TryParse<JobCommand>(name, out var jobCommand))
                {
                    var jobResult = collectedData.ToObject<JobResult>();
                    return jobResult;
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
            throw new NotImplementedException();
        }
    }
}
