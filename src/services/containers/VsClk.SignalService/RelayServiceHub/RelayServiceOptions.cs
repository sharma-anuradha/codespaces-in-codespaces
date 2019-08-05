using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Options defined for the Relay Service
    /// </summary>
    public class RelayServiceOptions
    {
        /// <summary>
        /// Identifier used for the backplane providers
        /// </summary>
        public string Id { get; set; }
    }
}
