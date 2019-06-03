using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace VsClk.EnvReg.Models.Errors
{
    public class HttpResponseStatusException : RemoteException
    {
        public HttpResponseStatusException(HttpResponseMessage httpResponseMessage, bool? clientOrigin)
            : base(MessageFormat(httpResponseMessage, clientOrigin), GetErrorCode(httpResponseMessage.StatusCode))
        {
            StatusCode = httpResponseMessage.StatusCode;
        }

        public HttpStatusCode StatusCode
        {
            get
            {
                object value = Data[nameof(StatusCode)];
                return value != null ? (HttpStatusCode)(int)value : default;
            }
            set => Data[nameof(StatusCode)] = (int)value;
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

        public string ReasonPhrase
        {
            get
            {
                object value = Data[nameof(ReasonPhrase)];
                return value != null ? (string)value : null;
            }
            set => Data[nameof(ReasonPhrase)] = value;
        }

        public bool? ClientOrigin
        {
            get
            {
                object value = Data[nameof(ClientOrigin)];
                return value != null ? (bool?)value : null;
            }
            set => Data[nameof(ClientOrigin)] = value;
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
                    string retryAfterMessage = "Please try again later.";
                    IEnumerable<string> retryAfterValues = response.Headers.GetValues(HeaderNames.RetryAfter);
                    IEnumerator<string> enumerator = retryAfterValues.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        string retryAfterValue = enumerator.Current;
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
