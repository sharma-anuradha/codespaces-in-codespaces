namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    ///  A contact reference entity
    /// </summary>
    public struct ContactReference
    {
        public ContactReference(string id, string connectionId)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Id = id;
            ConnectionId = connectionId;
        }

        /// <summary>
        /// The contact id to refer
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The optional connection id on this contact
        /// </summary>
        public string ConnectionId { get; set; }

        public override string ToString()
        {
            return $"{{ Id:{Id} connectionId:{ConnectionId} }}";
        }
    }
}
