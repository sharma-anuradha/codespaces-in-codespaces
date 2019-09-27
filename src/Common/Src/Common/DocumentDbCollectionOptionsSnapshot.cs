// <copyright file="DocumentDbCollectionOptionsSnapshot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Wrap <see cref="IOptions{DocumentDbCollectionOptions}"/> instance as <see cref="IOptionsSnapshot{DocumentDbCollectionOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public class DocumentDbCollectionOptionsSnapshot : IOptionsSnapshot<DocumentDbCollectionOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentDbCollectionOptionsSnapshot"/> class.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <param name="configureOptions">The documentdb options.</param>
        public DocumentDbCollectionOptionsSnapshot(
            IOptions<DocumentDbCollectionOptions> options, 
            Action<DocumentDbCollectionOptions> configureOptions)
        {
            configureOptions(options.Value);
            Options = options.Value;
        }

        /// <summary>
        /// Gets the options value.
        /// </summary>
        public DocumentDbCollectionOptions Value => Options;

        private DocumentDbCollectionOptions Options { get; }

        /// <summary>
        /// Get the options instance.
        /// </summary>
        /// <param name="name">Unused.</param>
        /// <returns>The options instance.</returns>
        public DocumentDbCollectionOptions Get(string name)
        {
            return Options;
        }
    }
}
