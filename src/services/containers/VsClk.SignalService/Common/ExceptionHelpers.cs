// <copyright file="ExceptionHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    public static class ExceptionHelpers
    {
        /// <summary>
        /// Returns an array of the entire exception list.
        /// </summary>
        /// <param name="exception">The original exception to work off.</param>
        /// <returns>Array of Exceptions from innermost to outermost.</returns>
        public static Exception[] GetInnerExceptions(this Exception exception)
        {
            Requires.NotNull(exception, nameof(exception));
            var exceptions = new HashSet<Exception>();
            AddExceptions(exceptions, exception);

            return exceptions.ToArray();
        }

        private static void AddExceptions(HashSet<Exception> exceptions, Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                foreach (var ex in aggregateException.InnerExceptions)
                {
                    AddExceptions(exceptions, ex);
                }
            }
            else
            {
                exceptions.Add(exception);
                if (exception.InnerException != null)
                {
                    AddExceptions(exceptions, exception.InnerException);
                }
            }
        }
    }
}
