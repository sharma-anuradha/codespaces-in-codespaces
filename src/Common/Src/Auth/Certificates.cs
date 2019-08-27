// <copyright file="Certificates.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.VsSaaS.Services.Common.Crypto.Utilities
{
    public class Certificates
    {
        /// <summary>
        /// Gets the private key contained in the bytes, extracts the private
        /// key, and returns it.
        /// It supports both PEM and X509 containers in the byte array.
        /// </summary>
        public static RSA GetRSAPrivateKey(byte[] certBytes)
        {
            // try to decode the RSA private key assiming it's a PEM
            if (RSAPrivateKeyDecoder.Decode(certBytes, out RSAParameters parameters))
            {
                var rsa = RSA.Create();
                rsa.ImportParameters(parameters);
                return rsa;
            }

            // that didn't work, so just assume we'll be loading an x509 with private key
            var cert = new X509Certificate2(certBytes);
            return cert.GetRSAPrivateKey();
        }

        /// <summary>
        /// Gets the private key contained in the bytes, extracts the private
        /// key, and returns it.
        /// It supports both PEM and X509 containers in the byte array.
        /// </summary>
        public static RSA GetRSAPublicKey(byte[] certBytes)
        {
            var cert = new X509Certificate2(certBytes);
            return cert.GetRSAPublicKey();
        }

        /// <summary>
        /// Generates a libtrust kid for the public key stored in the given file.
        /// </summary>
        public static string GenerateKidForPublicKey(byte[] certBytes)
        {
            return GenerateKidForPublicKey(GetRSAPublicKey(certBytes));
        }

        /// <summary>
        /// Generates a libtrust kid for the provided public key.
        /// </summary>
        public static string GenerateKidForPublicKey(RSA cert)
        {
            var rsa = cert.ExportParameters(false);

            // DER format. This is supposedly standard for public certificates?
            // See RFC5280 section 4.1.2.7.
            // The certificate that Windows generates doesn't hash to the
            // same value that libtrust recognizes, so we regenerate a new public key
            // here so that it can be hashed to create the kid.
            byte[] key = Asn.GenerateElementSequence(new byte[][]
            {
                Asn.GenerateElementSequence(new byte[][]
                {
                    // OID 1.2.840.113549.1.1.1: rsaEncryption
                    Asn.GenerateElementOid(new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 }),
                    Asn.GenerateElementNull()
                }),
                Asn.GenerateElementBitstring(
                    Asn.GenerateElementSequence(new byte[][]
                    {
                        Asn.GenerateElementInteger(rsa.Modulus),
                        Asn.GenerateElementInteger(rsa.Exponent)
                    })
                )
            });

            // https://github.com/docker/libtrust/blob/master/util.go#L194
            byte[] sha = SHA256.Create().ComputeHash(key);
            string b32 = Base32.ToBase32String(sha);

            var kid = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                kid.Append(b32.Substring(i * 4, 4));
                if (i != 11)
                {
                    kid.Append(':');
                }
            }

            return kid.ToString();
        }
    }
}
