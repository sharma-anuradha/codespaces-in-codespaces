// <copyright file="MonitorStateConverter.cs" company="Microsoft">
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
    public class MonitorStateConverter : JsonConverter<KeyValuePair<string, AbstractMonitorState>>
    {
        /// <inheritdoc/>
        public override KeyValuePair<string, AbstractMonitorState> ReadJson(JsonReader reader, Type objectType, KeyValuePair<string, AbstractMonitorState> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var monitorState = JObject.Load(reader);
            string key = (string)monitorState.First.First;
            var value = monitorState.Last.First;

            switch (key)
            {
                case nameof(LinuxDockerState):
                    var linuxDockerState = new LinuxDockerState
                    {
                        EnvironmentId = (string)value["EnvironmentId"],
                        Name = (string)value["Name"],
                        SessionPath = (string)value["SessionPath"],
                        State = value["State"].ToObject<EnvironmentRunningState>(),
                        TimeStamp = (DateTime)value["TimeStamp"],
                    };
                    return new KeyValuePair<string, AbstractMonitorState>(key, linuxDockerState);

                default:
                    return new KeyValuePair<string, AbstractMonitorState>(key, null);
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, KeyValuePair<string, AbstractMonitorState> value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
