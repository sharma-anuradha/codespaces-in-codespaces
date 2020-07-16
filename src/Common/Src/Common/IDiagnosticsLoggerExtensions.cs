// <copyright file="IDiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Diagnostics.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IDiagnosticsLogger"/>.
    /// Support for creating well-formed logging messages.
    /// </summary>
    public static class IDiagnosticsLoggerExtensions
    {
        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completion of the operation.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operation scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <returns>Returns the task.</returns>
        public static async Task OperationScopeWithThrowingCustomExceptionControlFlowAsync(
            this IDiagnosticsLogger logger,
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            Func<Exception, IDiagnosticsLogger, Task<bool>> errCallback)
        {
            var childLogger = logger.WithValues(new LogValueSet());
            var duration = Stopwatch.StartNew();

            try
            {
                await callback(childLogger);

                childLogger.FluentAddDuration(duration).LogInfo($"{name}_complete");
            }
            catch (Exception e)
            {
                var swallowException = false;
                try
                {
                    swallowException = await errCallback(e, childLogger);
                }
                catch (Exception ex)
                {
                    childLogger.FluentAddDuration(duration).FluentAddValue("InnerException", true).LogException($"{name}_error", ex);

                    // Any exception thrown from errCallback should not be swallowed.
                    throw;
                }
                finally
                {
                    childLogger.FluentAddDuration(duration).LogException($"{name}_error", e);
                }

                if (!swallowException)
                {
                    // Throw the current exception if the errCallback does not specify to swallow.
                    throw;
                }
            }
        }
    }
}
