using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class CSPReportInputFormatter : TextInputFormatter
    {
        public CSPReportInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/csp-report"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context,
            Encoding effectiveEncoding
        )
        {
            var body = context.HttpContext.Request.Body;
            CspReportRequest report = await System.Text.Json.JsonSerializer.DeserializeAsync<CspReportRequest>(body);
            return await InputFormatterResult.SuccessAsync(report);
        }

        private static async Task<string> ReadLineAsync(
            string expectedText, StreamReader reader, InputFormatterContext context,
            ILogger logger)
        {
            var line = await reader.ReadLineAsync();

            if (!line.StartsWith(expectedText))
            {
                var errorMessage = $"Looked for '{expectedText}' and got '{line}'";

                context.ModelState.TryAddModelError(context.ModelName, errorMessage);
                logger.LogError(errorMessage);

                throw new Exception(errorMessage);
            }

            return line;
        }
    }

    public class WorkbenchCspDynamicAttributes
    {
        public string RelayEndpoints { get; set; }
        public string InlineJavaScriptNonce { get; set; }
        public string LiveShareEndpoint { get; set; }
        public string WildcardApiEndpoint { get; set; }
        public string PartnerProxyApiEndpoint { get; set; }
        public string PartnerFaviconsEndpoint { get; set; }
        public string PartnerPortForwardingEndpoint { get; set; }
    }

    public class CspReportRequest
    {
        [JsonPropertyName("csp-report")]
        public CspReport CspReport { get; set; }
    }

    public class CspReport
    {
        [JsonPropertyName("document-uri")]
        public string DocumentUri { get; set; }

        [JsonPropertyName("referrer")]
        public string Referrer { get; set; }

        [JsonPropertyName("violated-directive")]
        public string ViolatedDirective { get; set; }

        [JsonPropertyName("effective-directive")]
        public string EffectiveDirective { get; set; }

        [JsonPropertyName("original-policy")]
        public string OriginalPolicy { get; set; }

        [JsonPropertyName("blocked-uri")]
        public string BlockedUri { get; set; }

        [JsonPropertyName("status-code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("source-file")]
        public string SourceFile { get; set; }

        [JsonPropertyName("line-number")]
        public int LineNumber { get; set; }

        [JsonPropertyName("column-number")]
        public int ColumnNumber { get; set; }
    }

    public static class WorkbenchCspHelpers {
        public static WorkbenchCspPartnerEndpoints GitHubCspEndpoints = new WorkbenchCspPartnerEndpoints
        {
            ProxyApiEndpoint = "https://api.github.com/vscs_internal/proxy/",
            FaviconsEndpoint = "https://github.com/favicons/",
        };

        public static readonly List<string> LiveShareDevRelayEndpoints = new List<string> {
            // primary
            "dev-ci-usw2-private-relay",
            "ppe-rel-usw2-private-relay",
            "ppe-rel-use-private-relay",
            "ppe-rel-use2-private-relay",
            "ppe-rel-eun-private-relay",
            "ppe-rel-asse-private-relay",
            // secondary
            "dev-ci-use-relay",
            "ppe-rel-usw2-relay",
            "ppe-rel-use-relay",
            "ppe-rel-euw-relay",
            "ppe-rel-asse-relay",
        };

        public static readonly List<string> LiveShareProdRelayEndpoints = new List<string> {
            // primary
            "prod-ins-usw2-private-relay",
            "prod-ins-use-private-relay",
            "prod-ins-use2-private-relay",
            "prod-ins-eun-private-relay",
            "prod-ins-asse-private-relay",
            "prod-rel-usw2-private-relay",
            "prod-rel-use2-private-relay",
            "prod-rel-eun-private-relay",
            "prod-rel-asse-private-relay",
            // secondary
            "prod-ins-usw2-relay",
            "prod-ins-use-relay",
            "prod-ins-euw-relay",
            "prod-ins-asse-relay",
            "prod-rel-usw2-relay",
            "prod-rel-use-relay",
            "prod-rel-euw-relay",
            "prod-rel-asse-relay",
        };
    }
}
