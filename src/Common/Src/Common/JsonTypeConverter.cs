// <copyright file="JsonTypeConverter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A json converter that will add a 'type' property to deserialize the object
    /// </summary>
    public abstract class JsonTypeConverter : JsonConverter
    {
        private const string TypeProperty = "type";

#pragma warning disable CA2326 // Do not use TypeNameHandling values other than None
#pragma warning disable CA2327 // Do not use insecure JsonSerializerSettings
        private static readonly JsonSerializer ObjectSerializer = JsonSerializer.Create(new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects });
#pragma warning restore CA2326 // Do not use TypeNameHandling values other than None
#pragma warning restore CA2327 // Do not use insecure JsonSerializerSettings

        public override bool CanConvert(Type objectType)
        {
            return BaseType.IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var readObject = serializer.Deserialize(reader);
            if (readObject is JObject jObject)
            {
                if (jObject.TryGetValue(TypeProperty, out JToken value))
                {
                    var typeValue = value.Value<string>();
                    if (SupportedTypes.TryGetValue(typeValue, out var type))
                    {
                        return jObject.ToObject(type);
                    }

                    throw new JsonReaderException($"type:{typeValue} not supported");
                }
                else
                {
                    throw new JsonReaderException("'type' property is missing from the json content.");
                }
            }

            // return the object with 'auto' handling
            return readObject;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null)
            {
                var type = value.GetType();
                var entry = SupportedTypes.FirstOrDefault(kvp => kvp.Value == type);
                if (entry.Value != null)
                {
                    var jObject = JObject.FromObject(value);
                    jObject.Add(TypeProperty, JToken.FromObject(entry.Key));
                    serializer.Serialize(writer, jObject);
                    return;
                }
            }

            // use default object serialization
            ObjectSerializer.Serialize(writer, value);
        }

        protected abstract Type BaseType { get; }

        protected abstract IDictionary<string, Type> SupportedTypes { get; }
    }
}
