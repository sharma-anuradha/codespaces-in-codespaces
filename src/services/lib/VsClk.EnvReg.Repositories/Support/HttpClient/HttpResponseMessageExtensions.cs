using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using VsClk.EnvReg.Models.Errors;

namespace VsClk.EnvReg.Repositories.Support.HttpClient
{
    public static class HttpResponseMessageExtensions
    {
        private static readonly string LiveShareServedByHeaderKey = "X-Ms-ServedBy";
        private static readonly string LiveShareServedByHeaderTargetValue = "VSLS";

        public static async Task ThrowIfFailedAsync(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.Content?.Headers?.ContentType?.MediaType == "application/json")
                {
                    try
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        var details = (JObject)JsonConvert.DeserializeObject(content);
                        ThrowVersionDetails(response.StatusCode, details);
                        ThrowServerJsonException(response.StatusCode, details);
                    }
                    catch (RemoteException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        // Suppress any unexpected exceptions from error-handling.
                        // Just ignore the error details if they couldn't be parsed.
                    }
                }

                ThrowHttpStatusException(response);
            }
        }

        private static void ThrowVersionDetails(HttpStatusCode status, JObject details)
        {
            if ((status == HttpStatusCode.NotFound || status == HttpStatusCode.Gone) &&
                details.Property("supportedApiVersion") != null)
            {
                if (status == HttpStatusCode.Gone)
                {
                    throw new RemoteException(
                        "This software requires an update for compatibility with the service.",
                        ErrorCodes.OlderThanServer);
                }
                else
                {
                    throw new RemoteException(
                        "This software requires a newer version of the service. " +
                        "Wait for the service to be updated, or configure a different endpoint.",
                        ErrorCodes.NewerThanServer);
                }
            }
        }

        private static void ThrowServerJsonException(HttpStatusCode status, JObject details)
        {
            var errorDetails = details.ToObject<ErrorDetails>();

            throw new RemoteInvocationException(
                errorDetails.Message,
                errorDetails.StackTrace);
        }

        private static void ThrowHttpStatusException(HttpResponseMessage response)
        {
            // this is a special case as we have 502 from a client perspective
            // vs. 502 from the server perspective.
            if (response.StatusCode == HttpStatusCode.BadGateway
                && !(response.Headers.TryGetValues(LiveShareServedByHeaderKey, out var servedByHeaderValue)
                    && servedByHeaderValue.FirstOrDefault() == LiveShareServedByHeaderTargetValue))
            {
                throw new HttpResponseStatusException(response, true);
            }

            throw new HttpResponseStatusException(response, null);
        }

        // TODO: Consider moving this class (and other response models) to a shared assembly.
        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        class ErrorDetails
        {
            public string Message { get; set; }

            public string StackTrace { get; set; }
        }
    }
}
