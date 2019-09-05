// <copyright file="QuickByteArrayIndexFinder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Text;

namespace Microsoft.VsSaaS.Services.Common.Crypto.Utilities
{
    // implements Boyer–Moore–Horspool algorithm
    // Reference: https://en.wikipedia.org/wiki/Boyer–Moore–Horspool_algorithm
    public class QuickByteArrayIndexFinder

    {
        private readonly int[] _skipTable = new int[256];
        private readonly string _boundary;

        public QuickByteArrayIndexFinder(string boundary)
        {
            this._boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            Initialize(boundary);
        }

        private void Initialize(string boundary)
        {
            BoundaryBytes = Encoding.ASCII.GetBytes(boundary);

            var length = BoundaryBytes.Length;
            for (var i = 0; i < this._skipTable.Length; ++i)
            {
                this._skipTable[i] = length;
            }
            for (var i = 0; i < length; ++i)
            {
                this._skipTable[BoundaryBytes[i]] = Math.Max(1, length - 1 - i);
            }
        }

        public int GetSkipValue(byte input)
        {
            return this._skipTable[input];
        }

        public bool SubMatch(byte[] segment, out int index)
        {
            index = 0;

            if (segment == null)
            {
                return false;
            }

            var matchBytesLengthMinusOne = BoundaryBytes.Length - 1;
            var matchBytesLastByte = BoundaryBytes[matchBytesLengthMinusOne];
            var segmentEndMinusMatchBytesLength = segment.Length - matchBytesLengthMinusOne;
            while (index < segmentEndMinusMatchBytesLength)
            {
                var lookaheadTailChar = segment[index + matchBytesLengthMinusOne];
                if (lookaheadTailChar == matchBytesLastByte &&
                    CompareBuffers(segment, index, BoundaryBytes, 0, matchBytesLengthMinusOne) == 0)
                {
                    return true;
                }
                index += GetSkipValue(lookaheadTailChar);
            }
            return false;
        }

        public static int CompareBuffers(byte[] buffer1, int offset1, byte[] buffer2, int offset2, int count)
        {
            for (; count-- > 0; offset1++, offset2++)
            {
                if (buffer1[offset1] != buffer2[offset2])
                {
                    return buffer1[offset1] - buffer2[offset2];
                }
            }
            return 0;
        }

        public byte[] BoundaryBytes { get; private set; }
    }
}
