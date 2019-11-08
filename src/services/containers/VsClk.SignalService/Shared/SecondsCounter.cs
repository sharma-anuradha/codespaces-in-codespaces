// <copyright file="SecondsCounter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// A simple seconds counter based on rate & update seconds
    /// </summary>
    internal struct SecondsCounter
    {
        private Func<bool> next;

        public SecondsCounter(int updateSecs, int rate)
        {
            int divider = updateSecs / rate;
            if (divider == 0)
            {
                Requires.Fail("divider == 0");
            }

            int counter = 0;
            this.next = () =>
            {
                counter = (counter + 1) % divider;
                return counter == 0;
            };
        }

        /// <summary>
        /// Invoked every update seconds
        /// </summary>
        /// <returns></returns>
        public bool Next() => this.next();
    }
}
