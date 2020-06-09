// <copyright file="NgrokApiException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok
{
    /// <summary>
    /// Exception thrown by Ngrok API.
    /// </summary>
    public class NgrokApiException : Exception
    {
        private const string MessageFormat =
            "Error calling Ngrok Local API | HttpStatusCode: {0} | NgrokErrorCode: {1} | Message: {2} | DetailedMessage: {3}";

        /// <summary>
        ///  Initializes a new instance of the <see cref="NgrokApiException"/> class.
        /// </summary>
        /// <param name="error">Error returned by Ngrok.</param>
        public NgrokApiException(ErrorResponse error)
            : base(string.Format(MessageFormat, error.HttpStatusCode, error.NgrokErrorCode, error.Message, error.Details.DetailedErrorMessage))
        {
        }
    }
}
