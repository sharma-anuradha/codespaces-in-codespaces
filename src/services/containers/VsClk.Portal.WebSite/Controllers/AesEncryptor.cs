using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    class AesEncryptor
    {
        public static string EncryptStringToBytes_Aes(string plainText, string aesKey, string aesIV)
        {
            // Check arguments.
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException("plainText");
            }

            byte[] encryptedBytes;
            
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = HexStringToByteArray(aesKey);
                aesAlg.IV = HexStringToByteArray(aesIV);
                aesAlg.Padding = PaddingMode.PKCS7;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encryptedBytes = msEncrypt.ToArray();
                    }
                }
            }

            encryptedBytes.ToString();

            var encrypted = System.Convert.ToBase64String(encryptedBytes);
            
            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public static string DecryptStringFromHex_Aes(string ciphedText, string aesKey, string aesIV)
        {

            byte[] cipherBytes = System.Convert.FromBase64String(ciphedText);
            // Check arguments.
            if (cipherBytes == null || cipherBytes.Length <= 0)
                throw new ArgumentNullException("cipherBytes");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = HexStringToByteArray(aesKey);
                aesAlg.IV = HexStringToByteArray(aesIV);
                aesAlg.Padding = PaddingMode.PKCS7;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;
        }

        private static string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder(Bytes.Length * 2);
            foreach (byte B in Bytes)
            {
                Result.Append(B.ToString("X2"));
            }

            return Result.ToString();
        }

        private static byte[] HexStringToByteArray(string Hex)
        {
            byte[] Bytes = new byte[Hex.Length / 2];
            int[] HexValue = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 
            0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

            for (int x = 0, i = 0; i < Hex.Length; i += 2, x += 1)
            {
                Bytes[x] = (byte)(HexValue[Char.ToUpper(Hex[i + 0]) - '0'] << 4 |
                                HexValue[Char.ToUpper(Hex[i + 1]) - '0']);
            }

            return Bytes;
        }

    }
}
