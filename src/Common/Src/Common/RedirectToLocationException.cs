// <copyright file="RedirectToLocationException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Redirect To Location Exception.
    /// </summary>
    public class RedirectToLocationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectToLocationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="owningStamp">Target owning stamp.</param>
        public RedirectToLocationException(string message, string owningStamp)
            : base(message)
        {
            OwningStamp = Requires.NotNull(owningStamp, nameof(owningStamp));
        }

        /// <summary>
        /// Gets the owning stamp.
        /// </summary>
        public string OwningStamp { get; }
    }
}
