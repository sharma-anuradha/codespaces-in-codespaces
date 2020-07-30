// <copyright file="PartnerCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Storage;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// An interface for partner submission to underlying storage infrastructure.
    /// </summary>
    public class PartnerCloudStorageClient : IPartnerCloudStorageClient
    {
        private readonly IStorageQueueCollection queue;
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerCloudStorageClient"/> class.
        /// </summary>
        /// <param name="queue">the queue.</param>
        /// <param name="logger">the logger.</param>
        public PartnerCloudStorageClient(
            IStorageQueueCollection queue,
            IDiagnosticsLogger logger)
        {
            this.queue = queue;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task PushPartnerQueueSubmission(PartnerQueueSubmission queueSubmission)
        {
            Requires.NotNull(queueSubmission, nameof(queueSubmission));
            await this.queue.AddAsync(queueSubmission.ToJson(), null, this.logger);
        }
    }
}
