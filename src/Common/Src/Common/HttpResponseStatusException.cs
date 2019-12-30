// <copyright file="HttpResponseStatusException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// An exception that indicates a non-success http response.
    /// </summary>
    public class HttpResponseStatusException : RemoteException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseStatusException"/> class.
        /// </summary>
        /// <param name="httpResponseMessage">The http response message.</param>
        /// <param name="clientOrigin">A value indicating whether the http requrest originated in a non-web client.</param>
        public HttpResponseStatusException(HttpResponseMessage httpResponseMessage, bool? clientOrigin)
            : base(MessageFormat(httpResponseMessage, clientOrigin), GetErrorCode(httpResponseMessage.StatusCode))
        {
            StatusCode = httpResponseMessage.StatusCode;
            ReasonPhrase = httpResponseMessage.ReasonPhrase;
            ClientOrigin = clientOrigin;
            RetryAfter = httpResponseMessage.Headers.RetryAfter?.Delta?.Seconds;
        }

        /// <summary>
        /// Gets or sets the http status code.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get => this.GetDataValue<HttpStatusCode>(nameof(StatusCode));
            set => this.SetDataValue(nameof(StatusCode), value);
        }

        /// <summary>
        /// Gets or sets the reason phrase.
        /// </summary>
        public string ReasonPhrase
        {
            get => this.GetDataValue<string>(nameof(ReasonPhrase));
            set => this.SetDataValue(nameof(ReasonPhrase), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the http request originated in an external client.
        /// </summary>
        public bool? ClientOrigin
        {
            get => this.GetDataValue<bool?>(nameof(ClientOrigin));
            set => this.SetDataValue(nameof(ClientOrigin), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the http request originated in an external client.
        /// </summary>
        public int? RetryAfter
        {
            get => this.GetDataValue<int?>(nameof(RetryAfter));
            set => this.SetDataValue(nameof(RetryAfter), value);
        }

        private static int GetErrorCode(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return ErrorCodes.UnauthorizedHttpStatusCode;
                case HttpStatusCode.Forbidden:
                    return ErrorCodes.ForbiddenHttpStatusCode;
                default:
                    return ErrorCodes.NonSuccessHttpStatusCodeReceived;
            }
        }

        private static string MessageFormat(HttpResponseMessage response, bool? clientOrigin)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.ServiceUnavailable:
                    return "The Visual Studio Online service is unavailable at this time. Please try again later.";
                case HttpStatusCode.Unauthorized:
                    return "Your sign in token has expired. Please sign out and in again to refresh it. If the issue persists, please log a bug.";
                case HttpStatusCode.NotFound:
                    return "The Visual Studio Online service was unable to find the requested record. If this issue persists, please raise a bug.";
                case HttpStatusCode.UseProxy:
                    return "Visual Studio Online has detected you are behind a proxy and that you need to take steps for Visual Studio Online to use it. To find out more about proxy setup, see http://aka.ms/vsls-docs/proxy.";
                case HttpStatusCode.ProxyAuthenticationRequired:
                    return "Visual Studio Online has detected you are behind an authenticated proxy and that you need to take steps for Visual Studio Online to use it. To find out more about proxy setup, see http://aka.ms/vsls-docs/proxy.";
                case HttpStatusCode.BadGateway:
                    if (!clientOrigin.GetValueOrDefault())
                    {
                        return "Visual Studio Online is having connectivity issues. Please try again and if this issue persists, raise a bug.";
                    }

                    return "Visual Studio Online is having trouble making an outbound HTTP connection. Please check your proxy settings or see https://aka.ms/vsls-http-connection-error for more details.";

                case (HttpStatusCode)429:
                    var retryAfterMessage = "Please try again later.";
                    var retryAfterValues = response.Headers.GetValues(HeaderNames.RetryAfter);
                    var enumerator = retryAfterValues.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        var retryAfterValue = enumerator.Current;
                        if (!string.IsNullOrEmpty(retryAfterValue))
                        {
                            retryAfterMessage = string.Format("Please try again in {0} seconds.", retryAfterValue);
                        }
                    }

                    return string.Format("The action has been attempted too many times. {0} If this issue persists, please log a bug.", retryAfterMessage);

                default:
                    return $"Visual Studio Online has experienced the following internal error: ({(int)response.StatusCode} - {response.ReasonPhrase}). If this issue persists, please raise a bug.";
            }
        }
    }
}
