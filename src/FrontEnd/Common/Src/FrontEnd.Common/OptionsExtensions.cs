// <copyright file="OptionsExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;

// TODO: move into the Common project!
namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Options extensions.
    /// </summary>
    public static class OptionsExtensions
    {
        /// <summary>
        /// Promote an <see cref="IOptions{TOptions}"/> instance to an <see cref="IOptionsSnapshot{TOptions}"/>.
        /// </summary>
        /// <typeparam name="TOptions">The options type.</typeparam>
        /// <param name="option">The options instance.</param>
        /// <returns>The options snapshot instance.</returns>
        public static IOptionsSnapshot<TOptions> PromoteToOptionSnapshot<TOptions>(this IOptions<TOptions> option)
            where TOptions : class, new()
        {
            return new DirectOptionsSnapshot<TOptions>(option.Value);
        }

        private class DirectOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions>
            where TOptions : class, new()
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DirectOptionsSnapshot{TOptions}"/> class.
            /// </summary>
            /// <param name="options">The options instance.</param>
            public DirectOptionsSnapshot(TOptions options)
            {
                Options = options;
            }

            /// <summary>
            /// Gets the options value.
            /// </summary>
            public TOptions Value => Options;

            private TOptions Options { get; }

            public TOptions Get(string name)
            {
                return Options;
            }
        }
    }
}
