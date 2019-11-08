using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// This class will implement IHubFormatProvider interface to properly format sensitive data
    /// </summary>
    public class DataFormatter : IDataFormatProvider, ICustomFormatter
    {
        private const int KeySizeInBytes = 256 / 8; // 256-bit key
        private const string Null = "<null>";

        private static byte[] KeyBytes;
        private static KeyedHashAlgorithm Hash;
        private static readonly object Lock = new object();

        static DataFormatter()
        {
            KeyBytes = GenerateKey();
            Hash = new HMACSHA256(KeyBytes);
        }

        string IDataFormatProvider.GetPropertyFormat(string propertyName)
        {
            return FormatHelpers.GetPropertyFormat(propertyName);
        }

        object IFormatProvider.GetFormat(Type formatType)
        {
            return (formatType == typeof(ICustomFormatter)) ? this : null;
        }

        string ICustomFormatter.Format(string format, object value, IFormatProvider formatProvider)
        {
            if (value == null)
            {
                return Null;
            }

            char dataFormat = char.MinValue;

            if (!string.IsNullOrEmpty(format))
            {
                dataFormat = format[0];
            }

            switch(dataFormat)
            {
                case 'T':
                    return FormatText(value.ToString());
                case 'E':
                    return FormatEmail(value.ToString());
                case 'K':
                    return "<token>";
                default:
                    return value.ToString();
            }
        }

        private static string GetShortHash(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            return GetShortHash(Encoding.UTF8.GetBytes(value));
        }

        private static string GetShortHash(byte[] value)
        {
            byte[] hash;
            lock (Hash)
            {
                hash = Hash.ComputeHash(value);
            }

            return (hash[0] << 24 | hash[1] << 16 | hash[2] << 8 | hash[3]).ToString("x8");
        }


        private static byte[] GenerateKey()
        {
            byte[] key = new byte[KeySizeInBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        private string FormatEmail(string value)
        {
            if (value == null)
            {
                return Null;
            }

            string[] parts = value.Split(new char[] { '@' });
            if (parts.Length == 2)
            {
                // Don't hash the domain name.
                return '<' + GetShortHash(parts[0]) + '@' + parts[1] + '>';
            }
            else
            {
                return FormatText(value);
            }
        }

        private string FormatText(string value)
        {
            if (value == null)
            {
                return Null;
            }
            else if (value.Length == 0)
            {
                return value;
            }

            string hash = GetShortHash(value);
            return $"<{value.Length}:{hash}>";
        }
    }
}
