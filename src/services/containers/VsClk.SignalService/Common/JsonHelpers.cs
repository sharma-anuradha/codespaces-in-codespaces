// <copyright file="JsonHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Helpers for the System.Text.Json namespace.
    /// </summary>
    public static class JsonHelpers
    {
        private static Type typeGenericDictionary = typeof(IDictionary<string, object>);

        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Normalize a json element to return a 'raw' object.
        /// </summary>
        /// <param name="jsonElement">The json element to convert.</param>
        /// <param name="argumentType">The desired type to be converted.</param>
        /// <returns>The unboxed object.</returns>
        public static object ConvertTo(JsonElement jsonElement, Type argumentType)
        {
            Requires.NotNull(argumentType, nameof(argumentType));

            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                var itemType = argumentType.GetElementType();
                var isDictionaryType = IsDictionaryType(itemType);
                var array = Array.CreateInstance(itemType, jsonElement.GetArrayLength());
                int index = 0;
                foreach (var item in jsonElement.EnumerateArray())
                {
                    if (isDictionaryType)
                    {
                        array.SetValue(ToDictionary(item), index);
                    }
                    else
                    {
                        array.SetValue(ToObject(item, itemType), index);
                    }

                    ++index;
                }

                return array;
            }
            else if (IsDictionaryType(argumentType))
            {
                return ToDictionary(jsonElement);
            }

            return ToObject(jsonElement, argumentType);
        }

        public static JsonElement FromObject(object value)
        {
            Requires.NotNull(value, nameof(value));

            if (value is JToken jToken)
            {
                return ToJsonElement(jToken.ToString());
            }

            var json = JsonSerializer.Serialize(value);
            return ToJsonElement(json);
        }

        public static object NormalizeObject(object value)
        {
            if (value == null || IsSimple(value.GetType()) || !IsComplexType(value.GetType()))
            {
                return value;
            }

            return FromObject(value);
        }

        public static JsonElement ToJsonElement(string json)
        {
            return JsonDocument.Parse(json).RootElement;
        }

        private static bool IsComplexType(Type type)
        {
            return IsDictionaryType(type) || typeof(JToken).IsAssignableFrom(type);
        }

        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive
              || type.Equals(typeof(string));
        }

        private static bool IsDictionaryType(Type type)
        {
            return typeGenericDictionary == type || typeGenericDictionary.IsAssignableFrom(type);
        }

        private static Dictionary<string, object> ToDictionary(JsonElement jsonElement)
        {
            var obj = new Dictionary<string, object>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                obj[property.Name] = ToObject(property.Value, ToType(property.Value.ValueKind));
            }

            return obj;
        }

        private static Type ToType(JsonValueKind jsonValueKind)
        {
            switch (jsonValueKind)
            {
                case JsonValueKind.Array:
                    return typeof(object[]);
                case JsonValueKind.String:
                    return typeof(string);
                case JsonValueKind.Number:
                    return typeof(double);
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return typeof(bool);
                default:
                    return typeof(object);
            }
        }

        private static object ToObject(JsonElement jsonElement, Type argumentType)
        {
            if (jsonElement.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (argumentType == typeof(bool))
            {
                return jsonElement.GetBoolean();
            }
            else if (argumentType == typeof(sbyte))
            {
                return jsonElement.GetSByte();
            }
            else if (argumentType == typeof(byte))
            {
                return jsonElement.GetByte();
            }
            else if (argumentType == typeof(ushort))
            {
                return jsonElement.GetUInt16();
            }
            else if (argumentType == typeof(short))
            {
                return jsonElement.GetInt16();
            }
            else if (argumentType == typeof(uint))
            {
                return jsonElement.GetUInt32();
            }
            else if (argumentType == typeof(int))
            {
                return jsonElement.GetInt32();
            }
            else if (argumentType == typeof(double))
            {
                return jsonElement.GetDouble();
            }
            else if (argumentType == typeof(string))
            {
                return jsonElement.GetString();
            }
            else if (argumentType == typeof(object))
            {
                var typeObj = ToType(jsonElement.ValueKind);
                if (typeObj != typeof(object))
                {
                    return ToObject(jsonElement, typeObj);
                }

                return ToDictionary(jsonElement);
            }

            var json = jsonElement.ToString();
            return JsonSerializer.Deserialize(json, argumentType, jsonSerializerOptions);
        }
    }
}
