// <copyright file="HttpBearerChallengeUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Helpers for working with HTTP Bearer Challenges.
    /// </summary>
    public static class HttpBearerChallengeUtils
    {
        /// <summary>
        /// Parses the WWW-Authenticate header from an HTTP Response Message.
        /// </summary>
        /// <param name="message">The http response message.</param>
        /// <returns>A dictionary of parameters parsed from the response.</returns>
        public static Dictionary<string, string> ParseWwwAuthenticateHeader(HttpResponseMessage message)
        {
            if (message.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException($"Expected 401 Unauthorized status code, actual status code {message.StatusCode}");
            }

            if (!message.Headers.WwwAuthenticate.Any())
            {
                throw new InvalidOperationException("WWW-Authenticate response header not found");
            }

            // RFC 2617 defines the format of the response parameters as "token "=" ( token | quoted-string )" Example:
            // Bearer authorization="https://login.microsoftonline.com/72F988BF-86F1-41AF-91AB-2D7CD011DB47", resource="https://serviceidentity.azure.net/"
            return message
                .Headers
                .WwwAuthenticate
                .Single()
                .Parameter
                .Split(", ")
                .Select(x => x.Split('='))
                .ToDictionary(k => k[0].Trim(), v => v[1].Trim('"'));
        }
    }
}
