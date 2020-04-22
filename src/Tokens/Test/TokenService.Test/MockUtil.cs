using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    internal static class MockUtil
    {
        public static HttpContext MockHttpContext(
            AuthenticationHeaderValue authHeader)
        {
            var headers = new HeaderDictionary();
            if (authHeader != null)
            {
                headers.Add("Authorization", authHeader.ToString());
            }

            var mockRequest = new Mock<HttpRequest>(MockBehavior.Strict);
            mockRequest.Setup((r) => r.Headers).Returns(headers);

            var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
            mockContext.Setup((c) => c.Request).Returns(mockRequest.Object);
            return mockContext.Object;
        }

        public static HttpClient MockHttpClient(
            Func<HttpRequestMessage, HttpResponseMessage> requestHandler)
        {
            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(
                    (request, cancellation) => Task.FromResult(requestHandler(request)));
            return new HttpClient(mockMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost"),
            };
        }

        public static HttpClient MockHttpClient(object responseObject)
        {
            Assert.NotNull(responseObject);
            var json = JsonSerializer.Serialize(responseObject);
            return MockHttpClient((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json),
                };
            });
        }

        public static T MockHttpClientProvider<T>(HttpClient httpClient)
            where T : class, IHttpClientProvider
        {
            var mockHttpClientProvider = new Mock<T>();
            mockHttpClientProvider
                .Setup((p) => p.HttpClient)
                .Returns(httpClient);
            return mockHttpClientProvider.Object;
        }

        public static IOptionsMonitor<T> MockOptionsMonitor<T>(T options = null) where T: class, new()
        {
            options ??= new T();
            var mockOptions = new Mock<IOptionsMonitor<T>>(MockBehavior.Strict);
            mockOptions
                .Setup((o) => o.Get(It.IsAny<string>()))
                .Returns(options);
            return mockOptions.Object;
        }

        public static ILoggerFactory MockLoggerFactory()
        {
            var mockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            mockLoggerFactory
                .Setup((f) => f.CreateLogger(It.IsAny<string>()))
                .Returns(new Mock<ILogger>(MockBehavior.Loose).Object);
            return mockLoggerFactory.Object;
        }
    }
}
