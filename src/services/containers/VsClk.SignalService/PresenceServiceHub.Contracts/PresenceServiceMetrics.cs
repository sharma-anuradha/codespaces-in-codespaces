namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Define the service metrics
    /// </summary>
    public struct PresenceServiceMetrics
    {
        public PresenceServiceMetrics(
            int count,
            int selfCount,
            int totalSelfCount,
            int stubCount)
        {
            Count = count;
            SelfCount = selfCount;
            TotalSelfCount = totalSelfCount;
            StubCount = stubCount;
        }

        public int Count { get; }
        public int SelfCount { get; }
        public int TotalSelfCount { get; }

        public int StubCount { get; }
    }
}
