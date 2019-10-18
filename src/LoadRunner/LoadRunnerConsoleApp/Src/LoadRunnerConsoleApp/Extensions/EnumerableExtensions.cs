using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Extensions
{
    /// <summary>
    /// Enumerable Extensions.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Loops through each item in a list.
        /// </summary>
        /// <typeparam name="T">List item type.</typeparam>
        /// <param name="source">Target source.</param>
        /// <param name="action">Target action.</param>
        /// <param name="delay">Target delay between iterations.</param>
        /// <returns>Resulting task.</returns>
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, TimeSpan? delay = null)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (source == null)
            {
                return;
            }

            foreach (var item in source)
            {
                await action(item);

                if (delay != null)
                {
                    await Task.Delay((int)delay.Value.TotalMilliseconds);
                }
            }
        }
    }
}
