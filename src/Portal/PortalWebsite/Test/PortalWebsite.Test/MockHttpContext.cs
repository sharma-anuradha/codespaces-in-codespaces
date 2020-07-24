using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortalWebsite.Test
{
    public class MockHttpContext : HttpContext
    {
        private MockHttpContext()
        {
            Request = new MockHttpRequest(this);

            MockResponse = new Mock<HttpResponse>();

            var responseHeaders = new HeaderDictionary();
            MockResponse.SetupGet(p => p.Headers).Returns(responseHeaders);

            MockResponseCookies = new Mock<IResponseCookies>();
            MockResponse.SetupGet(p => p.Cookies).Returns(MockResponseCookies.Object);

            Response = MockResponse.Object;
        }

        class MockHttpRequest : HttpRequest
        {
            public MockHttpRequest(HttpContext context)
            {
                HttpContext = context;
                Headers = new HeaderDictionary();

                var mockCookies = new Mock<IRequestCookieCollection>();
                Cookies = mockCookies.Object;
            }

            public override Stream Body { get; set; }
            public override long? ContentLength { get; set; }
            public override string ContentType { get; set; }
            public override IRequestCookieCollection Cookies { get; set; }
            public override IFormCollection Form { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override bool HasFormContentType => throw new NotImplementedException();

            public override IHeaderDictionary Headers { get; }

            public override HostString Host { get; set; }

            public override HttpContext HttpContext { get; }

            public override bool IsHttps { get; set; }
            public override string Method { get; set; }
            public override PathString Path { get; set; }
            public override PathString PathBase { get; set; }
            public override string Protocol { get; set; }
            public override IQueryCollection Query { get; set; }
            public override QueryString QueryString { get; set; }
            public override string Scheme { get; set; }

            public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        public override ConnectionInfo Connection => throw new NotImplementedException();

        public override IFeatureCollection Features => throw new NotImplementedException();

        public override IDictionary<object, object> Items { get => new Dictionary<object, object> { ["CorrelationId"] = "Test" }; set => throw new NotImplementedException(); }

        public override HttpRequest Request { get; }
        public Mock<HttpResponse> MockResponse { get; }
        public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IServiceProvider RequestServices { get; set; }

        public override HttpResponse Response { get; }

        public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override WebSocketManager WebSockets => throw new NotImplementedException();

        public Mock<IResponseCookies> MockResponseCookies { get; }

        public static MockHttpContext Create()
        {
            var context = new MockHttpContext();
            context.Request.Method = "GET";
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("testhost");
            context.Request.PathBase = new PathString(string.Empty);
            context.Request.Path = new PathString("/test/path");
            context.Request.QueryString = new QueryString(string.Empty);

            return context;
        }

        public override void Abort()
        {
            throw new NotImplementedException();
        }
    }
}
