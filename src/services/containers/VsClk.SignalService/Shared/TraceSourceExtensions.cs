//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Convenience methods for TraceSource.TraceEvent().
    /// </summary>
    /// <remarks>
    /// Methods that take format arguments or (dollar-prefixed) interpolated strings
    /// delay the formatting of arguments until the trace event is actually consumed
    /// by a trace listener (assuming it's not ignored by a trace filter).
    /// </remarks>
    internal static class TraceSourceExtensions
    {
        /// <summary>
        /// Creates a new TraceSource with listeners and switch copied from the
        /// existing TraceSource.
        /// </summary>
        public static TraceSource WithName(this TraceSource trace, string name)
        {
            var newTraceSource = new TraceSource(name);
            newTraceSource.Listeners.Clear(); // Remove the DefaultTraceListener
            newTraceSource.Listeners.AddRange(trace.Listeners);
            newTraceSource.Switch = trace.Switch;
            return newTraceSource;
        }

        /// <summary>
        /// Return a new TraceSource by appending a name 
        /// </summary>
        public static TraceSource WithAppendName(this TraceSource trace, string name)
        {
            return WithName(trace, trace.Name + name);
        }

        /// <summary>
        /// Returns true if there is at least one listener that will consume this
        /// trace event type.
        /// </summary>
        public static bool ListensFor(this TraceSource trace, TraceEventType eventType)
        {
            foreach (TraceListener listener in trace.Listeners)
            {
                if (listener.GetType() == typeof(DefaultTraceListener))
                {
                    continue;
                }
                if (listener.Filter == null || listener.Filter.ShouldTrace(
                    null, trace.Name, eventType, 0, null, null, null, null))
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<T> FindListeners<T>(this TraceSource trace)
            where T : TraceListener
        {
            return trace.Listeners.OfType<T>();
        }

        public static T FindListener<T>(this TraceSource trace) where T : TraceListener
        {
            return trace.Listeners.OfType<T>().FirstOrDefault();
        }

        [Conditional("TRACE")]
        public static void Critical(this TraceSource trace, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Critical, 0, message);

        [Conditional("TRACE")]
        public static void Critical(this TraceSource trace, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Critical, 0, format, args);

        [Conditional("TRACE")]
        public static void Critical(this TraceSource trace, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Critical, 0, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void Error(this TraceSource trace, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Error, 0, message);

        [Conditional("TRACE")]
        public static void Error(this TraceSource trace, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Error, 0, format, args);

        [Conditional("TRACE")]
        public static void Error(this TraceSource trace, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Error, 0, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void Warning(this TraceSource trace, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Warning, 0, message);

        [Conditional("TRACE")]
        public static void Warning(this TraceSource trace, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Warning, 0, format, args);

        [Conditional("TRACE")]
        public static void Warning(this TraceSource trace, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Warning, 0, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void Info(this TraceSource trace, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Information, 0, message);

        [Conditional("TRACE")]
        public static void Info(this TraceSource trace, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Information, 0, format, args);

        [Conditional("TRACE")]
        public static void Info(this TraceSource trace, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Information, 0, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void Verbose(this TraceSource trace, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Verbose, 0, message);

        [Conditional("TRACE")]
        public static void Verbose(this TraceSource trace, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Verbose, 0, format, args);

        [Conditional("TRACE")]
        public static void Verbose(this TraceSource trace, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Verbose, 0, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void CriticalEvent(this TraceSource trace, int id, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Critical, id, message);

        [Conditional("TRACE")]
        public static void CriticalEvent(this TraceSource trace, int id, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Critical, id, format, args);

        [Conditional("TRACE")]
        public static void CriticalEvent(this TraceSource trace, int id, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Critical, id, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void ErrorEvent(this TraceSource trace, int id, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Error, id, message);

        [Conditional("TRACE")]
        public static void ErrorEvent(this TraceSource trace, int id, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Error, id, format, args);

        [Conditional("TRACE")]
        public static void ErrorEvent(this TraceSource trace, int id, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Error, id, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void WarningEvent(this TraceSource trace, int id, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Warning, id, message);

        [Conditional("TRACE")]
        public static void WarningEvent(this TraceSource trace, int id, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Warning, id, format, args);

        [Conditional("TRACE")]
        public static void WarningEvent(this TraceSource trace, int id, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Warning, id, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void InfoEvent(this TraceSource trace, int id, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Information, id, message);

        [Conditional("TRACE")]
        public static void InfoEvent(this TraceSource trace, int id, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Information, id, format, args);

        [Conditional("TRACE")]
        public static void InfoEvent(this TraceSource trace, int id, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Information, id, message.Format, message.GetArguments());

        [Conditional("TRACE")]
        public static void VerboseEvent(this TraceSource trace, int id, NonFormattableString message) =>
            trace.TraceEvent(TraceEventType.Verbose, id, message);

        [Conditional("TRACE")]
        public static void VerboseEvent(this TraceSource trace, int id, NonFormattableString format, params object[] args) =>
            trace.TraceEvent(TraceEventType.Verbose, id, format, args);

        [Conditional("TRACE")]
        public static void VerboseEvent(this TraceSource trace, int id, FormattableString message) =>
            trace.TraceEvent(TraceEventType.Verbose, id, message.Format, message.GetArguments());

        /// <summary>
        /// Work around an overload resolution problem with `FormattableString`, enabling
        /// interpolated string to be used with tracing efficiently while also supporting
        /// plain strings.
        /// </summary>
        /// <remarks>
        /// See <a href="https://stackoverflow.com/questions/35770713/overloaded-string-methods-with-string-interpolation">
        /// Overloaded string methods with string interpolation</a>.
        ///
        /// An extra implicit conversion causes the compiler to prefer the overloads that take
        /// `FormattableString` when interpolated strings are used with the tracing methods. While
        /// plain `string` parameters get converted to/from this struct, the conversions should
        /// basically evaporate in inlining.
        /// </remarks>
        public struct NonFormattableString
        {
            private NonFormattableString(string s) { String = s; }
            public string String { get; }
            public static implicit operator string(NonFormattableString nfs) => nfs.String;
            public static implicit operator NonFormattableString(string s) => new NonFormattableString(s);
            public static implicit operator NonFormattableString(FormattableString s) =>
                throw new InvalidOperationException("This conversion should not be selected by overload resolution.");
        }
    }
}
