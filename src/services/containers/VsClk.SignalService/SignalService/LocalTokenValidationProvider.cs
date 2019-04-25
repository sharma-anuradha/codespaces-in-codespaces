using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements ITokenValidationProvider based on local deployed certificates and app settings
    /// </summary>
    public class LocalTokenValidationProvider : ITokenValidationProvider
    {
        private LocalTokenValidationProvider(string issuer, string audience, SecurityKey[] securityKeys)
        {
            Issuer = issuer;
            Audience = audience;
            SecurityKeys = securityKeys;
        }

        public static ITokenValidationProvider Create(IConfigurationSection appSettings)
        {
            var systemHost = appSettings.GetValue<string>("SystemHost");
            var securityKeys = GetSecurityKeySet(appSettings);

            if (!string.IsNullOrEmpty(systemHost) && securityKeys.Length > 0)
            {
                var issuer = $"https://{systemHost}/";
                return new LocalTokenValidationProvider(issuer, issuer, securityKeys);
            }

            return null;
        }

        public string Audience { get; }
        public string Issuer { get; }

        public SecurityKey[] SecurityKeys { get; }

        private static SecurityKey[] GetSecurityKeySet(IConfigurationSection appSettings)
        {
            var securityKeys = new List<SecurityKey>();

            var publicKeys = appSettings.GetSection("PublicKeys");
            foreach (var item in publicKeys.GetChildren())
            {
                byte[] publicKeyBytes = LoadCertifcateContent(item.Value);
                var securityKey = new X509SecurityKey(new X509Certificate2(publicKeyBytes));
                securityKeys.Add(securityKey);
            }

            return securityKeys.ToArray();
        }

        private static byte[] LoadCertifcateContent(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
            {
                throw new ArgumentNullException(nameof(keyPath));
            }
            if (!File.Exists(keyPath))
            {
                throw new InvalidOperationException($"Path specified doesn't exist: '{keyPath}'");
            }
            return File.ReadAllBytes(keyPath);
        }
    }
}
