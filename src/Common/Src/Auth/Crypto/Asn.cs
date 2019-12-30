// <copyright file="Asn.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Linq;

#pragma warning disable SA1108 // Block statemens shojld not contain embedded comments
#pragma warning disable SA1600 // Elements should be dcoumented
#pragma warning disable SA1602 // Enumeration items should be dcoumented
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1616 // Element return value should have text
#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.VsSaaS.Services.Common.Crypto.Utilities
{
    /// <summary>
    /// Code used to generate ASN.1 encoded content.
    /// http://luca.ntop.org/Teaching/Appunti/asn1.html
    /// </summary>
    public static class Asn
    {
        public enum Tag : byte
        {
            Integer = 0x02,
            BitString = 0x03,
            Null = 0x05,
            Oid = 0x06,
            Sequence = 0x30,
        }

        /// <summary>
        /// Generates a header for the given content.
        /// </summary>
        /// <returns></returns>
        public static byte[] GenerateHeader(Tag tag, int contentLength)
        {
            // The number of bytes needed to represent the length of the element.
            byte lengthLength = 0;
            var lengthTemp = contentLength;
            while (lengthTemp > 0)
            {
                lengthTemp >>= 8;
                lengthLength++;
            }

            var headerLength = 2; // Tag is one byte, first size byte is one byte.
            if (contentLength > 0x7F) // If the length is more than 127, the first size byte represents the length of the length.
            {
                headerLength += lengthLength;
            }

            var ret = new byte[headerLength];
            var index = 0;
            ret[index++] = (byte)tag;

            if (contentLength > 0x7F)
            {
                ret[index++] = (byte)(0x80 + lengthLength);
                lengthTemp = contentLength;
                for (var i = lengthLength - 1; i >= 0; i--)
                {
                    // Most significant byte comes first.
                    ret[index++] = (byte)((lengthTemp >> (i * 8)) & 0xFF);
                }
            }
            else
            {
                ret[index++] = (byte)contentLength;
            }

            return ret;
        }

        public static byte[] GenerateElementOid(byte[] bytes)
        {
            var header = GenerateHeader(Tag.Oid, bytes.Length);

            var ret = new byte[header.Length + bytes.Length];
            Array.Copy(header, 0, ret, 0, header.Length);
            Array.Copy(bytes, 0, ret, header.Length, bytes.Length);

            return ret;
        }

        public static byte[] GenerateElementNull()
        {
            return new byte[] { (byte)Tag.Null, 0x00 };
        }

        public static byte[] GenerateElementBitstring(byte[] bits)
        {
            // Need one zero byte for bitstring.
            var header = GenerateHeader(Tag.BitString, bits.Length + 1);

            var ret = new byte[header.Length + bits.Length + 1];
            Array.Copy(header, 0, ret, 0, header.Length);
            ret[header.Length] = 0x00;
            Array.Copy(bits, 0, ret, header.Length + 1, bits.Length);

            return ret;
        }

        public static byte[] GenerateElementSequence(byte[][] values)
        {
            var totalContentLength = values.Sum(x => x.Length);
            var header = GenerateHeader(Tag.Sequence, totalContentLength);

            var ret = new byte[header.Length + totalContentLength];
            Array.Copy(header, 0, ret, 0, header.Length);

            var currentIndex = header.Length;
            foreach (var element in values)
            {
                Array.Copy(element, 0, ret, currentIndex, element.Length);
                currentIndex += element.Length;
            }

            return ret;
        }

        public static byte[] GenerateElementInteger(byte[] number)
        {
            // In ASN.1, the modulus is stored in twos-complement. This means that if
            // the first bit of the number is set, we need to pad it with a zero byte
            // in order to make sure it is not interpreted as a negative number.
            var needsZeroPadding = (number[0] & 0x80) == 0x80;
            var contentLength = number.Length + (needsZeroPadding ? 1 : 0);

            var header = GenerateHeader(Tag.Integer, contentLength);

            var ret = new byte[header.Length + contentLength];
            Array.Copy(header, 0, ret, 0, header.Length);
            if (needsZeroPadding)
            {
                ret[header.Length] = 0x00;
                Array.Copy(number, 0, ret, header.Length + 1, number.Length);
            }
            else
            {
                Array.Copy(number, 0, ret, header.Length, number.Length);
            }

            return ret;
        }
    }
}
