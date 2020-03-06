using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class CachedCosmosDbSystemConfigurationRepositoryTests
    {
        private const BindingFlags DefaultBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        [Fact]
        public async Task GetAsync_CachesAndUsesMissingValues()
        {
            var options = new DocumentDbCollectionOptions();
            CachedCosmosDbSystemConfigurationRepository.ConfigureOptions(options);

            Assert.True(options.CacheMissingValues);

            var optionsWrapper = new Mock<IOptionsMonitor<DocumentDbCollectionOptions>>();
            optionsWrapper.Setup(x => x.CurrentValue).Returns(options);

            var mockClient = new Mock<IDocumentClient>();
            mockClient
                .SetupSequence(x => x.ReadDocumentAsync<SystemConfigurationRecord>(It.IsAny<Uri>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Throws(NewDocumentClientException(HttpStatusCode.NotFound))
                .Returns(() =>
                {
                    // Note this uses SetupSequence - if ReadDocumentAsync is called twice the test should fail
                    Assert.True(false, "Document should not be read, cached value should be used");
                    return null;
                });

            var clientProvider = MockClientProvider<SystemConfigurationRecord>(mockClient.Object);

            var healthProvider = new Mock<IHealthProvider>().Object;
            var loggerFactory = new DefaultLoggerFactory();
            var logValueSet = new LogValueSet();

            var cache = new InMemoryManagedCache(loggerFactory, logValueSet);

            var db = new CachedCosmosDbSystemConfigurationRepository(
                optionsWrapper.Object,
                clientProvider,
                healthProvider,
                loggerFactory,
                logValueSet,
                cache);

            Assert.Equal(0, cache.ItemCount);

            await db.GetAsync("key", loggerFactory.New());

            Assert.Equal(1, cache.ItemCount);

            var result = await db.GetAsync("key", loggerFactory.New());

            Assert.Null(result);
        }

        private static IDocumentDbClientProvider MockClientProvider<T>(IDocumentClient client = null)
        {
            var mock = new Mock<IDocumentDbClientProvider>();
            mock.Setup(x => x.DatabaseId).Returns("mock");

            if (client == null)
            {
                var mockClient = new Mock<IDocumentClient>();

                mockClient
                    .Setup(x => x.ReadDocumentAsync<T>(It.IsAny<Uri>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                    .Throws(NewDocumentClientException(HttpStatusCode.NotFound));

                client = mockClient.Object;
            }

            mock.Setup(x => x.GetClientAsync()).ReturnsAsync(client);

            return mock.Object;
        }

        public static DocumentResponse<T> ToResourceResponse<T>(T resource, HttpStatusCode statusCode, IDictionary<string, string> responseHeaders = null)
        {
            var headers = new NameValueCollection { { "x-ms-request-charge", "0" } };

            if (responseHeaders != null)
            {
                foreach (var responseHeader in responseHeaders)
                {
                    headers[responseHeader.Key] = responseHeader.Value;
                }
            }

            var headersDictionaryType = Type.GetType("Microsoft.Azure.Documents.Collections.DictionaryNameValueCollection, Microsoft.Azure.DocumentDB.Core, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            var headersDictionaryInstance = Activator.CreateInstance(headersDictionaryType, headers);

            var documentServiceResponseType = Type.GetType("Microsoft.Azure.Documents.DocumentServiceResponse, Microsoft.Azure.DocumentDB.Core, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            var documentServiceResponse = CreateInstance(documentServiceResponseType, Stream.Null, headersDictionaryInstance, statusCode, null);

            var resourceResponse = new DocumentResponse<T>(resource);

            var responseField = typeof(DocumentResponse<T>).GetTypeInfo().GetField("response", DefaultBindingFlags);           
            responseField?.SetValue(resourceResponse, documentServiceResponse);

            return resourceResponse;
        }

        public static DocumentClientException NewDocumentClientException(HttpStatusCode statusCode, IDictionary<string, string> responseHeaders = null)
        {
            var headers = new Dictionary<string, string> { { "x-ms-request-charge", "0" } };

            if (responseHeaders != null)
            {
                foreach (var responseHeader in responseHeaders)
                {
                    headers[responseHeader.Key] = responseHeader.Value;
                }
            }

            var httpRespHeadersType = typeof(HttpResponseHeaders);
            var httpRespHeaders = CreateInstance(httpRespHeadersType) as HttpResponseHeaders;

            foreach (var header in headers)
            {
                httpRespHeaders.Add(header.Key, header.Value);
            }

            var exceptionType = typeof(DocumentClientException);
            var exception = CreateInstance(exceptionType, new Error(), httpRespHeaders, statusCode);

            return exception as DocumentClientException;
        }

        private static object CreateInstance(Type type, params object[] args)
        {
            return type.GetTypeInfo().GetConstructors(DefaultBindingFlags)[0].Invoke(args);
        }
    }
}
