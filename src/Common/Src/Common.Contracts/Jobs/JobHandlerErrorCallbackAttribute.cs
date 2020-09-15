// <copyright file="JobHandlerErrorCallbackAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Attribute to apply for error callback types
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class JobHandlerErrorCallbackAttribute : Attribute
    {
        public JobHandlerErrorCallbackAttribute(Type errorTypeCallback)
        {
            ErrorTypeCallback = errorTypeCallback;
        }

        /// <summary>
        /// Gets the error type callback.
        /// </summary>
        public Type ErrorTypeCallback { get; }
    }
}
