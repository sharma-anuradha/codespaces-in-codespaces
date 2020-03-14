// <copyright file="Buffer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
#pragma warning disable CA1710 // Rename 'Buffer' to end in 'Collection'
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1201 // Elements should appear in the correct order

    /// <summary>
    /// Represents a segment of a byte array.
    /// </summary>
    /// <remarks>
    /// This structure is similar to ArraySegment&lt;byte&gt;, with several additional
    /// conveniences.
    /// </remarks>
    [DebuggerDisplay("{ToString(),nq}")]
    internal struct Buffer : IEquatable<Buffer>
    {
        public static readonly Buffer Empty = default;
        private static readonly byte[] EmptyArray = System.Array.Empty<byte>();

        private readonly byte[] array;

        public Buffer(int size)
            : this(new byte[size], 0, size)
        {
        }

        private Buffer(byte[] array, int offset, int count)
        {
            this.array = array;
            this.Offset = offset;
            this.Count = count;
        }

        public static Buffer From(byte[] array) => From(array, 0, array?.Length ?? 0);

        public static Buffer From(byte[] array, int offset, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (offset < 0 || offset > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || offset + count > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return new Buffer(array, offset, count);
        }

#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] Array => this.array ?? EmptyArray;
#pragma warning restore CA1819 // Properties should not return arrays

        public int Offset { get; }

        public int Count { get; }

        public byte this[int index]
        {
            get => Array[Offset + index];
            set => Array[Offset + index] = value;
        }

        public Buffer Slice(int offset, int count)
        {
            return new Buffer(Array, Offset + offset, count);
        }

        public void CopyTo(Buffer other, int otherOffset = 0)
        {
            if (other.Count - otherOffset < Count)
            {
                throw new ArgumentException("Destination buffer is too small.", nameof(other));
            }

            System.Array.Copy(Array, Offset, other.Array, other.Offset + otherOffset, Count);
        }

        public byte[] ToArray()
        {
            // Make a new copy even if the current buffer is using exactly the whole array,
            // because buffers are often re-used while the ToArray() snapshot is saved.
            var newBuffer = new Buffer(Count);
            CopyTo(newBuffer);
            return newBuffer.Array;
        }

        public static implicit operator Buffer(byte[] array)
        {
            return Buffer.From(array ?? System.Array.Empty<byte>());
        }

        public override bool Equals(object obj)
        {
            return obj is Buffer otherBuffer && Equals(otherBuffer);
        }

        public bool Equals(Buffer other)
        {
            if (Count != other.Count)
            {
                // Buffers are different sizes.
                return false;
            }

            if (Array == other.Array && Offset == other.Offset)
            {
                // A buffer instance is being compared to itself.
                return true;
            }

            bool equal = true;
            int end = Count + Offset;
            for (int i = Offset, j = other.Offset; i < end; i++, j++)
            {
                equal &= Array[i] == other.Array[j];
            }

            return equal;
        }

        public static bool operator ==(Buffer left, Buffer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Buffer left, Buffer right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return new ArraySegment<byte>(Array, Offset, Count).GetHashCode();
        }
    }
}
