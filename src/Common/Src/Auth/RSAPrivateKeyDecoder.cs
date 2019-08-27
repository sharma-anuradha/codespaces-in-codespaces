// <copyright file="RSAPrivateKeyDecoder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VsSaaS.Services.Common.Crypto.Utilities
{

    public class RSAPrivateKeyDecoder
    {
        private static QuickByteArrayIndexFinder PrivateKeyHeaderFinder = new QuickByteArrayIndexFinder("-----BEGIN PRIVATE KEY-----\n");

        private static QuickByteArrayIndexFinder PrivateKeyFooterFinder = new QuickByteArrayIndexFinder("-----END PRIVATE KEY-----");

        private static QuickByteArrayIndexFinder RSAPrivateKeyHeaderFinder = new QuickByteArrayIndexFinder("-----BEGIN RSA PRIVATE KEY-----\n");

        private static QuickByteArrayIndexFinder RSAPrivateKeyFooterFinder = new QuickByteArrayIndexFinder("-----END RSA PRIVATE KEY-----");

        // 1.2.840.113549.1.1.1 - RSA encryption, including the sequence byte and terminal encoded null
        private static readonly byte[] OIDRSAEncryption = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };

        public static bool Decode(byte[] privateKey, out RSAParameters parameters)
        {
            if (PrivateKeyHeaderFinder.SubMatch(privateKey, out int privateKeyHeaderIndex) &&
                PrivateKeyFooterFinder.SubMatch(privateKey, out int privateKeyFooterIndex) &&
                privateKeyHeaderIndex < privateKeyFooterIndex)
            {
                var body = ExtractDecodedBody(
                    privateKey,
                    privateKeyHeaderIndex + PrivateKeyHeaderFinder.BoundaryBytes.Length,
                    privateKeyFooterIndex
                );
                return DecodePrivateKey(body, out parameters);
            }


            if (RSAPrivateKeyHeaderFinder.SubMatch(privateKey, out int rsaPrivateKeyHeaderIndex) &&
                RSAPrivateKeyFooterFinder.SubMatch(privateKey, out int rsaPrivateKeyFooterIndex) &&
                rsaPrivateKeyHeaderIndex < rsaPrivateKeyFooterIndex)
            {
                var body = ExtractDecodedBody(
                    privateKey,
                    rsaPrivateKeyHeaderIndex + RSAPrivateKeyHeaderFinder.BoundaryBytes.Length,
                    rsaPrivateKeyFooterIndex
                );
                return DecodeRSAPrivateKey(body, out parameters);
            }

            parameters = default;
            return false;
        }

        private static byte[] ExtractDecodedBody(byte[] privateKey, int bodyStart, int bodyEnd)
        {
            var encodedBody = new byte[bodyEnd - bodyStart];
            Array.ConstrainedCopy(privateKey, bodyStart, encodedBody, 0, encodedBody.Length);
            return Convert.FromBase64String(Encoding.ASCII.GetString(encodedBody));
        }

        private static bool DecodePrivateKey(byte[] privateKey, out RSAParameters parameters)
        {
            parameters = default;
            // read the asn.1 encoded SubjectPublicKeyInfo blob
            var memoryStream = new MemoryStream(privateKey);
            int streamLength = (int)memoryStream.Length;

            using (var reader = new BinaryReader(memoryStream))
            {
                ushort twobytes = reader.ReadUInt16();
                if (twobytes == 0x8130) // data read as little endian order (actual data order for Sequence is 30 81)
                {
                    reader.ReadByte(); // advance 1 byte
                }
                else if (twobytes == 0x8230)
                {
                    reader.ReadInt16(); // advance 2 bytes
                }
                else
                {
                    return false;
                }

                byte bt = reader.ReadByte();
                if (bt != 0x02)
                {
                    return false;
                }

                twobytes = reader.ReadUInt16();
                if (twobytes != 0x0001)
                {
                    return false;
                }

                byte[] seq = reader.ReadBytes(15);
                if (QuickByteArrayIndexFinder.CompareBuffers(seq, 0, OIDRSAEncryption, 0, seq.Length) != 0) // make sure Sequence for OID is correct
                {
                    return false;
                }

                bt = reader.ReadByte();
                if (bt != 0x04) // expect an Octet string 
                {
                    return false;
                }

                bt = reader.ReadByte(); // read next byte, or next 2 bytes is  0x81 or 0x82; otherwise bt is the byte count
                if (bt == 0x81)
                {
                    reader.ReadByte();
                }
                else if (bt == 0x82)
                {
                    reader.ReadUInt16();
                }

                // at this stage, the remaining sequence should be the RSA private key
                byte[] rsaprivkey = reader.ReadBytes((int)(streamLength - memoryStream.Position));
                return DecodeRSAPrivateKey(rsaprivkey, out parameters);
            }
        }

        private static bool DecodeRSAPrivateKey(byte[] privateKey, out RSAParameters parameters)
        {
            parameters = default;
            // decode the asn.1 encoded RSA private key
            var memoryStream = new MemoryStream(privateKey);
            using (var reader = new BinaryReader(memoryStream))
            {
                ushort twobytes = reader.ReadUInt16();
                if (twobytes == 0x8130) // data read as little endian order (actual data order for Sequence is 30 81)
                {
                    reader.ReadByte(); // advance 1 byte
                }
                else if (twobytes == 0x8230)
                {
                    reader.ReadInt16(); // advance 2 bytes
                }
                else
                {
                    return false;
                }

                twobytes = reader.ReadUInt16();
                if (twobytes != 0x0102) // version number
                {
                    return false;
                }

                byte bt = reader.ReadByte();
                if (bt != 0x00)
                {
                    return false;
                }

                // all private key components are Integer sequences
                parameters = new RSAParameters();

                int elems = GetIntegerSize(reader);
                parameters.Modulus = reader.ReadBytes(elems);

                elems = GetIntegerSize(reader);
                parameters.Exponent = reader.ReadBytes(elems);

                elems = GetIntegerSize(reader);
                parameters.D = reader.ReadBytes(elems);

                elems = GetIntegerSize(reader);
                parameters.P = reader.ReadBytes(elems);

                elems = GetIntegerSize(reader);
                parameters.Q = reader.ReadBytes(elems);

                elems = GetIntegerSize(reader);
                parameters.DP = reader.ReadBytes(elems);

                elems = GetIntegerSize(reader);
                parameters.DQ = reader.ReadBytes(elems);

                elems = GetIntegerSize(reader);
                parameters.InverseQ = reader.ReadBytes(elems);

                return true;
            }
        }

        private static int GetIntegerSize(BinaryReader reader)
        {
            int count;
            byte bt = reader.ReadByte();
            if (bt != 0x02) // expect integer
            {
                return 0;
            }
            bt = reader.ReadByte();

            switch (bt)
            {
                case 0x81:
                    count = reader.ReadByte(); // data size in next byte
                    break;
                case 0x82:
                    byte highbyte = reader.ReadByte();
                    byte lowbyte = reader.ReadByte();
                    byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                    count = BitConverter.ToInt32(modint, 0);
                    break;
                default:
                    count = bt; // we already have the data size
                    break;
            }

            while (reader.ReadByte() == 0x00)
            {
                // remove high order zeros in data
                count -= 1;
            }

            reader.BaseStream.Seek(-1, SeekOrigin.Current); // last ReadByte wasn't arrayA removed zero, so back up arrayA byte
            return count;
        }
    }
}
