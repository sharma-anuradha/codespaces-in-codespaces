// <copyright file="ExponentialBackoff.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Helper class to calculate a backoff delay.
    /// </summary>
    internal struct ExponentialBackoff
    {
        private readonly int maxRetries;
        private readonly int delayMilliseconds;
        private readonly int maxDelayMilliseconds;
        private int retries;
        private int pow;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> struct.
        /// </summary>
        /// <param name="maxRetries">Max retries.</param>
        /// <param name="delayMilliseconds">Delay in milli seconds.</param>
        /// <param name="maxDelayMilliseconds">Maximum delay to reach.</param>
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

        /// <summary>
        /// Gets current retries.
        /// </summary>
        public int Retries => this.retries;

        /// <summary>
        /// Calculate next delay in milli seconds.
        /// </summary>
        /// <returns>Milli seconds delay.</returns>
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

            return Math.Min(
                this.delayMilliseconds * (this.pow - 1) / 2,
                this.maxDelayMilliseconds);
        }
    }
}
