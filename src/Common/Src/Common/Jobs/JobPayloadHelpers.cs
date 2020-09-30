// <copyright file="JobPayloadHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Job payload helpers.
    /// </summary>
    public static class JobPayloadHelpers
    {
        private const string TypeFieldName = "Type";

        private static readonly ConcurrentDictionary<Type, string> CachedTypes = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<string, Type> NameTypes = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// Convert a job payload to json format.
        /// </summary>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <returns>The json format.</returns>
        public static string ToJson(this JobPayload jobPayload)
        {
            Requires.NotNull(jobPayload, nameof(jobPayload));

            return JsonConvert.SerializeObject(jobPayload);
        }

        /// <summary>
        /// Deserialize a json content into a job payload.
        /// </summary>
        /// <param name="tagType">The payload tag type.</param>
        /// <param name="json">The json content.</param>
        /// <returns>An instance of a job payload.</returns>
        public static JobPayload FromJson(string tagType, string json)
        {
            Requires.NotNullOrEmpty(tagType, nameof(tagType));
            Requires.NotNullOrEmpty(json, nameof(json));

            Type payloadType;
            if (!NameTypes.TryGetValue(tagType, out payloadType))
            {
                throw new Exception($"tag:{tagType} not registered");
            }

            return (JobPayload)JsonConvert.DeserializeObject(json, payloadType);
        }

        /// <summary>
        /// Register a job payload type to be later used when deserializing.
        /// </summary>
        /// <param name="jobPayloadType">The job payload type.</param>
        /// <returns>Name of the tag.</returns>
        public static string RegisterPayloadType(this Type jobPayloadType)
        {
            Requires.NotNull(jobPayloadType, nameof(jobPayloadType));

            return GetTypeTag(jobPayloadType);
        }

        /// <summary>
        /// Return the type tag for a payload type.
        /// </summary>
        /// <param name="type">Type of the payload.</param>
        /// <returns>Name of the tag.</returns>
        public static string GetTypeTag(Type type)
        {
            return CachedTypes.GetOrAdd(type, (t) =>
            {
                var typeTag = GetTypeTagInternal(t);
                if (!NameTypes.TryAdd(typeTag, type))
                {
                    throw new Exception($"type tag:'{typeTag}' already exists");
                }

                return typeTag;
            });
        }

        private static string GetTypeTagInternal(Type type)
        {
            var fieldInfo = type.GetField(TypeFieldName);
            if (fieldInfo == null)
            {
                var typeName = GetTypeName(type)
                    .Replace('<', '_')
                    .Replace('>', '_')
                    .Replace('.', '_')
                    .Replace('+', '_');

                // remove the last '_'
                if (typeName.EndsWith('_'))
                {
                    typeName = typeName.Remove(typeName.Length - 1);
                }

                return typeName;
            }

            return fieldInfo.GetRawConstantValue().ToString();
        }

        private static string GetTypeName(Type type)
        {
            string friendlyName = type.Name;
            var payloadAttribute = type.GetCustomAttributes(typeof(JobPayloadAttribute), true).Cast<JobPayloadAttribute>().FirstOrDefault();
            if (payloadAttribute != null)
            {
                if (!string.IsNullOrEmpty(payloadAttribute.TypeName))
                {
                    friendlyName = payloadAttribute.TypeName;
                }
                else if (payloadAttribute.NameOption != JobPayloadNameOption.None)
                {
                    if (payloadAttribute.NameOption == JobPayloadNameOption.Name)
                    {
                        friendlyName = type.Namespace.Length > 0
                            ? type.FullName.Substring(type.Namespace.Length + 1)
                            : type.FullName;
                    }
                    else if (payloadAttribute.NameOption == JobPayloadNameOption.FullName)
                    {
                        friendlyName = type.FullName;
                    }
                }
            }

            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }

                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetTypeName(typeParameters[i]);
                    friendlyName += i == 0 ? typeParamName : "," + typeParamName;
                }

                friendlyName += ">";
            }

            return friendlyName;
        }
    }
}
