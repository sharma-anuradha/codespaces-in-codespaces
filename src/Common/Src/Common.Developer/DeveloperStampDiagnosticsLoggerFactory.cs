// <copyright file="DeveloperStampDiagnosticsLoggerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Developer.DevStampLogger
{
    /// <summary>
    /// Dev logger factory.
    /// </summary>
    public class DeveloperStampDiagnosticsLoggerFactory : IDiagnosticsLoggerFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeveloperStampDiagnosticsLoggerFactory"/> class.
        /// </summary>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public DeveloperStampDiagnosticsLoggerFactory(IResourceNameBuilder resourceNameBuilder, IServiceProvider serviceProvider)
        {
            ResourceNameBuilder = resourceNameBuilder;
            ServiceProvider = serviceProvider;
        }

        private TextWriter KustoStreamWriter { get; set; }

        private TextWriter LogFileStreamWriter { get; set; }

        private IServiceProvider ServiceProvider { get; set; }

        private IResourceNameBuilder ResourceNameBuilder { get; set; }

        /// <inheritdoc/>
        public IDiagnosticsLogger New(LogValueSet logValueSet = default)
        {
            if (KustoStreamWriter == default)
            {
                var controlPlaneAccessor = ServiceProvider.GetService<IControlPlaneAzureResourceAccessor>();
                KustoStreamWriter = new KustoStreamWriter(ResourceNameBuilder, controlPlaneAccessor);
            }

            if (LogFileStreamWriter == default)
            {
                LogFileStreamWriter = new LogFileStreamWriter();
            }

            return new DeveloperStampDiagnosticsLogger(logValueSet, new List<TextWriter>() { KustoStreamWriter, LogFileStreamWriter });
        }
    }
}
