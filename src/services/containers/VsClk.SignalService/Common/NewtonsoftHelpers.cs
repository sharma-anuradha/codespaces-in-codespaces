// <copyright file="NewtonsoftHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Newtonsoft namespace utils.
    /// </summary>
    public static class NewtonsoftHelpers
    {
        /// <summary>
        /// Cast a JToken instance to a specific type.
        /// </summary>
        /// <param name="jToken">The jtoken instance.</param>
        /// <param name="argumentType">Type to be converted.</param>
        /// <returns>Unboxed object with the specified type.</returns>
        public static object ToObject(JToken jToken, Type argumentType)
        {
            Requires.NotNull(jToken, nameof(jToken));

            if (argumentType == typeof(object) && jToken is JObject jObject)
            {
                return ToDictionary(jObject);
            }

            return jToken.ToObject(argumentType);
        }

        /// <summary>
        /// Converts a JObject root object to a 'raw' ictionary.
        /// </summary>
        /// <param name="jObject">A JObject instance.</param>
        /// <returns>The converted dictionary.</returns>
        public static Dictionary<string, object> ToDictionary(JObject jObject)
        {
            Requires.NotNull(jObject, nameof(jObject));

            return ((IDictionary<string, JToken>)jObject).ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                    {
                        if (kvp.Value is JObject jObjectValue)
                        {
                            return ToDictionary(jObjectValue);
                        }

                        return kvp.Value.ToObject<object>();
                    });
        }

        /// <summary>
        /// Unbox a potential Newtonsoft object type.
        /// </summary>
        /// <param name="o">The object instance.</param>
        /// <returns>The unboxed object.</returns>
        public static object ToRawObject(object o)
        {
            if (o is JObject jObject)
            {
                return ToDictionary(jObject);
            }
            else if (o is JToken jToken)
            {
                return jToken.ToObject<object>();
            }

            return o;
        }
    }
}
