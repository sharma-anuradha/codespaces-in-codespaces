// <copyright file="ProductInfoHeaderHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Adds product info about the service to the user-agent request header.
    /// </summary>
    public class ProductInfoHeaderHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductInfoHeaderHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler.</param>
        /// <param name="productInfo">The product header info to be added as the
        /// user agent header value on requests.</param>
        public ProductInfoHeaderHandler(
            HttpMessageHandler innerHandler,
            ProductInfoHeaderValue productInfo)
            : base(innerHandler)
        {
            ProductInfo = Requires.NotNull(productInfo, nameof(productInfo));
        }

        /// <summary>
        /// Gets the product info that is added to all requests.
        /// </summary>
        public ProductInfoHeaderValue ProductInfo { get; }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.UserAgent.Add(ProductInfo);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
