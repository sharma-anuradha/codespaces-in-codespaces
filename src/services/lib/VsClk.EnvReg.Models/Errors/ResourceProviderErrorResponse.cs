using System;
using System.Collections.Generic;
using System.Text;

namespace VsClk.EnvReg.Models.Errors
{
    /// <summary>
    /// The error class used by the ResourceProvider to send properly formatted responses back to RPaaS.
    /// </summary>
    public class ResourceProviderErrorResponse
    {
        /// <summary>
        /// Gets or sets the status of the request.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the error object.
        /// </summary>
        public ResourceProviderErrorInfo Error { get; set; }
    }

    public class ResourceProviderErrorInfo
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; }
    }
}
