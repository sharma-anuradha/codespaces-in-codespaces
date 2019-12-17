using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Contains utility functions to process error messages received from environments.
    /// </summary>
    public static class MessageCodeUtils
    {
        private const string CODE = "CODE";

        /// <summary>
        /// Looks for an error code contained in the list of errors.
        /// </summary>
        /// <param name="errors">The list of errors.</param>
        /// <returns>The error code, null otherwise.</returns>
        public static string GetCodeFromError(List<string> errors)
        {
            if (errors != null && errors.Any(x => x.Contains(CODE)))
            {
                return errors.FirstOrDefault(x => x.Contains(CODE)).Split(':')[1];
            }

            return null;
        }
    }
}
