// <copyright file="ClaimsConverter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// These rules don't handle nullable `?` and `!` annotations properly.
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Supports serializing an array of Claims to/from JSON in JWT format.
    /// </summary>
    /// <remarks>
    /// .NET represents claims as a list of Claim objects, where multi-valued claims
    /// are represented as multiple Claim objects with the same type. JWT represents
    /// claims as a dictionary, where multi-valued claims are represented as a single
    /// entry with an array of values.
    /// </remarks>
    internal class ClaimsConverter : JsonConverter<Claim[]?>
    {
        /// <inheritdoc/>
        public override Claim[]? ReadJson(
            JsonReader reader,
            Type objectType,
            Claim[]? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var claimsDictionary = serializer.Deserialize<IDictionary<string, object>>(reader)!;
            var claims = new List<Claim>();

            foreach (var claimPair in claimsDictionary)
            {
                if (claimPair.Value is IList<object> || claimPair.Value is IList<JToken>)
                {
                    // Convert from an array of claim values to multiple Claim objects.
                    foreach (var value in (IEnumerable)claimPair.Value)
                    {
                        claims.Add(new Claim(
                            claimPair.Key,
                            Convert.ToString(value, CultureInfo.InvariantCulture),
                            GetClaimValueType(value)));
                    }
                }
                else
                {
                    claims.Add(new Claim(
                        claimPair.Key,
                        Convert.ToString(claimPair.Value, CultureInfo.InvariantCulture),
                        GetClaimValueType(claimPair.Value)));
                }
            }

            return claims.ToArray();
        }

        /// <inheritdoc/>
        public override void WriteJson(
            JsonWriter writer,
            Claim[]? claims,
            JsonSerializer serializer)
        {
            if (claims == null)
            {
                writer.WriteNull();
                return;
            }

            var claimsDictionary = new Dictionary<string, object>();

            foreach (var claim in claims)
            {
                var claimValue = (object)claim.Value;
                if (claim.ValueType == ClaimValueTypes.Integer)
                {
                    claimValue = Convert.ToInt64((string)claimValue, CultureInfo.InvariantCulture);
                }

                if (claimsDictionary.TryGetValue(claim.Type, out object existingValue))
                {
                    // Convert from multiple Claim objects to an array of claim values.
                    var claimValueList = existingValue as IList<object>;
                    if (claimValueList == null)
                    {
                        claimValueList = new List<object>(2);
                        claimValueList.Add(existingValue);
                        claimsDictionary[claim.Type] = claimValueList;
                    }

                    claimValueList.Add(claimValue);
                }
                else
                {
                    claimsDictionary.Add(claim.Type, claimValue);
                }
            }

            serializer.Serialize(writer, claimsDictionary);
        }

        private static string GetClaimValueType(object value)
        {
            return value switch
            {
                int _ => ClaimValueTypes.Integer,
                long _ => ClaimValueTypes.Integer,
                JToken jValue when jValue.Type == JTokenType.Integer
                    => ClaimValueTypes.Integer,
                _ => ClaimValueTypes.String,
            };
        }
    }
}
