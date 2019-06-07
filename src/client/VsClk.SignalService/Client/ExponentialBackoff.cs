using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Helper class to calculate a backoff delay
    /// </summary>
    public struct ExponentialBackoff
    {
        private readonly int maxRetries, delayMilliseconds, maxDelayMilliseconds;
        private int retries, pow;

        public ExponentialBackoff(
            int maxRetries,
            int delayMilliseconds,
            int maxDelayMilliseconds)
        {
            this.maxRetries = maxRetries;
            this.delayMilliseconds = delayMilliseconds;
            this.maxDelayMilliseconds = maxDelayMilliseconds;
            this.retries = 0;
            this.pow = 1;
        }

        public int Retries => this.retries;

        public int NextDelayMilliseconds()
        {
            if (this.maxRetries != -1 && this.retries == this.maxRetries)
            {
                throw new TimeoutException("Max retry attempts exceeded.");
            }
            ++this.retries;
            if (this.retries < 31)
            {
                this.pow = this.pow << 1; // m_pow = Pow(2, m_retries - 1)
            }

            return Math.Min(this.delayMilliseconds * (this.pow - 1) / 2,
                this.maxDelayMilliseconds);
        }
    }
}
