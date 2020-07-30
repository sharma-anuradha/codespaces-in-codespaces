// <copyright file="HttpClientBase{TOptions}.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient
{
    /// <summary>
    /// Http client base class.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public abstract class HttpClientBase<TOptions> : HttpClientBase
        where TOptions : class, IHttpClientProviderOptions, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientBase{TOptions}"/> class.
        /// </summary>
        /// <param name="httpClientProvider">Http client provider.</param>
        public HttpClientBase(IHttpClientProvider<TOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }
    }
}
