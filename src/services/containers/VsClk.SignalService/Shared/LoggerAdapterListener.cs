// <copyright file="LoggerAdapterListener.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// A generic trace listener that will allow route trace messages into an ILogger instance
    /// </summary>
    internal class LoggerAdapterListener : TraceListener
    {
        private readonly Action<TraceEventType, string> loggerCallback;

        public LoggerAdapterListener(Action<TraceEventType, string> loggerCallback, IFormatProvider formatProvider)
        {
            this.loggerCallback = Requires.NotNull(loggerCallback, nameof(loggerCallback));
            FormatProvider = formatProvider;
        }

        public IFormatProvider FormatProvider { get; }

        public override void TraceEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string message)
        {
            if (Filter == null || Filter.ShouldTrace(
                eventCache, source, eventType, id, message, null, null, null))
            {
                WriteEvent(eventCache, source, eventType, id, message);
            }
        }

        public override void TraceEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string formatOrMessage,
            params object[] args)
        {
            if (Filter == null || Filter.ShouldTrace(
                eventCache, source, eventType, id, formatOrMessage, args, null, null))
            {
                string message = args == null ?
                    formatOrMessage : string.Format(FormatProvider, formatOrMessage, args);
                WriteEvent(eventCache, source, eventType, id, message);
            }
        }

        public override void TraceData(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            object data)
        {
            if (Filter == null || Filter.ShouldTrace(
                eventCache, source, eventType, id, null, null, data, null))
            {
                string message = string.Format(FormatProvider, "{0}", data);
                WriteEvent(eventCache, source, eventType, id, message);
            }
        }

        public override void Write(string line)
        {
            throw new NotSupportedException();
        }

        public override void WriteLine(string line)
        {
            throw new NotSupportedException();
        }

        private void WriteEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string message)
        {
            this.loggerCallback(eventType, message);
        }
    }

}
