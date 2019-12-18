// <copyright file="ErrorCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Remote error codes.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// Could not connect to server.
        /// </summary>
        public const int CouldNotConnectToServer = -32000;

        /// <summary>
        /// Older than server.
        /// </summary>
        public const int OlderThanServer = -32001;

        /// <summary>
        /// Newer than server.
        /// </summary>
        public const int NewerThanServer = -32002;

        /// <summary>
        /// Non-success http status code received.
        /// </summary>
        public const int NonSuccessHttpStatusCodeReceived = -32030;

        /// <summary>
        /// Unauthorized http status code received.
        /// </summary>
        public const int UnauthorizedHttpStatusCode = -32032;

        /// <summary>
        /// Forbidden http status code received.
        /// </summary>
        public const int ForbiddenHttpStatusCode = -32033;

        /// <summary>
        /// Invocation exception.
        /// </summary>
        public const int InvocationException = -32098;
    }
}
