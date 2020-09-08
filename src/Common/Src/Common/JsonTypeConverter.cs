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

        public override bool CanConvert(Type objectType)
        {
            return BaseType.IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var eventObject = (JObject)serializer.Deserialize(reader);
            if (eventObject.TryGetValue(TypeProperty, out JToken value))
            {
                var type = GetType(value.Value<string>());
                if (type != null)
                {
                    return eventObject.ToObject(type);
                }
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jObject = JObject.FromObject(value);
            var type = value.GetType();
            var entry = SupportedTypes.FirstOrDefault(kvp => kvp.Value == type);
            if (entry.Value != null)
            {
                jObject.Add(TypeProperty, JToken.FromObject(entry.Key));
            }

            serializer.Serialize(writer, jObject);
        }

        protected abstract Type BaseType { get; }

        protected abstract IDictionary<string, Type> SupportedTypes { get; }

        private Type GetType(string eventType)
        {
            Type type;
            if (SupportedTypes.TryGetValue(eventType, out type))
            {
                return type;
            }

            return null;
        }
    }
}
