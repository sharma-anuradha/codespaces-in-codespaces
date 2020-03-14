// <copyright file="ProxyBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Proxy base class.
    /// </summary>
    public class ProxyBase : IServiceProxyBase, IDisposable
    {
        private readonly List<IDisposable> onHubHandlers = new List<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyBase"/> class.
        /// </summary>
        /// <param name="hubProxy">The hub proxy instance.</param>
        /// <param name="trace">Trace instance.</param>
        /// <param name="formatProvider">Optional format provider.</param>
        protected ProxyBase(IHubProxy hubProxy, TraceSource trace, IFormatProvider formatProvider)
        {
            HubProxy = Requires.NotNull(hubProxy, nameof(hubProxy));
            Trace = Requires.NotNull(trace, nameof(trace));
            FormatProvider = formatProvider != null ? DataFormatProvider.Create(formatProvider) : null;
        }

        /// <inheritdoc/>
        public IHubProxy HubProxy { get; }

        /// <summary>
        /// Gets the proxy trace.
        /// </summary>
        protected TraceSource Trace { get; }

        /// <summary>
        /// Gets the proxy format provider.
        /// </summary>
        protected IDataFormatProvider FormatProvider { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var disposable in this.onHubHandlers)
            {
                disposable.Dispose();
            }

            this.onHubHandlers.Clear();
        }

        /// <summary>
        /// Attempt to cast an expected dictionary from the underlying hub proxy.
        /// </summary>
        /// <param name="argument">The raw argument.</param>
        /// <returns>A dictionary of type (string, object).</returns>
        protected static Dictionary<string, object> ToProxyDictionary(object argument)
        {
            if (argument == null)
            {
                return null;
            }

            if (argument.GetType() == typeof(Dictionary<string, object>))
            {
                return (Dictionary<string, object>)argument;
            }
            else if (argument.GetType() == typeof(Dictionary<object, object>))
            {
                return ((Dictionary<object, object>)argument).ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Attempt to convert an argument to an enum.
        /// </summary>
        /// <typeparam name="T">Type of enum to convert</typeparam>
        /// <param name="argument">The raw argument.</param>
        /// <returns>The enum type we expect.</returns>
        protected static T ToProxyEnum<T>(object argument)
            where T : struct
        {
            if (argument.GetType() == typeof(T))
            {
                return (T)argument;
            }
            else
            {
                if (Enum.TryParse<T>(argument.ToString(), out var value))
                {
                    return value;
                }

                throw new InvalidCastException();
            }
        }

        /// <summary>
        /// Convert the argument to an expected type.
        /// </summary>
        /// <typeparam name="T">Type we expect.</typeparam>
        /// <param name="argument">The raw argument.</param>
        /// <returns>The type we expected.</returns>
        protected static T ToProxyType<T>(object argument)
        {
            if (argument.GetType() == typeof(T))
            {
                return (T)argument;
            }
            else
            {
                return (T)Convert.ChangeType(argument, typeof(T));
            }
        }

        /// <summary>
        /// Add a hub handler.
        /// </summary>
        /// <param name="disposable">Disposable handler.</param>
        protected void AddHubHandler(IDisposable disposable)
        {
            this.onHubHandlers.Add(disposable);
        }
    }
}
