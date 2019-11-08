namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Options defined for the presence Service
    /// </summary>
    public class HubServiceOptions
    {
        /// <summary>
        /// Identifier used for the backplane providers
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Stamp where this service is running
        /// </summary>
        public string Stamp { get; set; }
    }
}
