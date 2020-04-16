// <copyright file="CryptoRandom.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Security.Cryptography;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Crypto Random implementation adapted from https://docs.microsoft.com/en-us/archive/msdn-magazine/2007/september/net-matters-tales-from-the-cryptorandom.
    /// </summary>
    public class CryptoRandom : Random
    {
        private RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        private byte[] uint32Buffer = new byte[4];

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoRandom"/> class.
        /// </summary>
        public CryptoRandom()
        {
        }

        /// <inheritdoc/>
        public override int Next()
        {
            provider.GetBytes(uint32Buffer);
            return BitConverter.ToInt32(uint32Buffer, 0) & 0x7FFFFFFF;
        }

        /// <inheritdoc/>
        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException("maxValue");
            }

            return Next(0, maxValue);
        }

        /// <inheritdoc/>
        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue");
            }

            if (minValue == maxValue)
            {
                return minValue;
            }

            long diff = maxValue - minValue;
            while (true)
            {
                provider.GetBytes(uint32Buffer);
                uint rand = BitConverter.ToUInt32(uint32Buffer, 0);

                long max = 1 + (long)uint.MaxValue;
                long remainder = max % diff;
                if (rand < max - remainder)
                {
                    return (int)(minValue + (rand % diff));
                }
            }
        }

        /// <inheritdoc/>
        public override double NextDouble()
        {
            provider.GetBytes(uint32Buffer);
            uint rand = BitConverter.ToUInt32(uint32Buffer, 0);
            return rand / (1.0 + uint.MaxValue);
        }

        /// <inheritdoc/>
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            provider.GetBytes(buffer);
        }
    }
}
