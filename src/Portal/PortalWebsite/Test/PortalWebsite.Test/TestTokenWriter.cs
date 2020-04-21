using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Resources;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortalWebsite.Test
{
    class TestTokenWriter
    {
        public const string TestIssuer1 = nameof(TestIssuer1);

        public const string TestAudience1 = nameof(TestAudience1);
        public static readonly SigningCredentials TestSigningCredentials1 =
            new X509SigningCredentials(GetTestCert("TestIssuer", isPrivate: true));
        public static readonly SigningCredentials TestValidatingCredentials1 =
            new X509SigningCredentials(GetTestCert("TestIssuer", isPrivate: false));
        public static readonly EncryptingCredentials TestEncryptingCredentials1 =
            new X509EncryptingCredentials(GetTestCert("TestAudience", isPrivate: false));

        public static X509Certificate2 GetTestCert(string name, bool isPrivate)
        {
            byte[] certBytes;
            var assembly = typeof(TestTokenWriter).Assembly;
            string resourceName = $"{assembly.GetName().Name}.{name}.pfx";
            using (var certStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (certStream == null)
                {
                    throw new MissingManifestResourceException($"Missing resource: {resourceName}");
                }

                certBytes = new byte[certStream.Length];
                certStream.Read(certBytes, 0, (int)certStream.Length);
            }

            var cert = new X509Certificate2(certBytes);
            Assert.True(cert.HasPrivateKey);

            if (!isPrivate)
            {
                certBytes = cert.Export(X509ContentType.Cert);
                cert = new X509Certificate2(certBytes);
                Assert.False(cert.HasPrivateKey);
            }

            return cert;
        }

        public static string WriteToken(JwtPayload payload, IDiagnosticsLogger logger)
        {
            var tokenWriter = new JwtWriter();
            tokenWriter.AddIssuer(TestIssuer1, TestSigningCredentials1);

            tokenWriter.AddAudience(TestAudience1);

            payload.AddClaims(new[]
            {
                new Claim(JwtRegisteredClaimNames.Iss, TestIssuer1),
                new Claim(JwtRegisteredClaimNames.Aud, TestAudience1),
                JwtWriter.CreateDateTimeClaim(
                        JwtRegisteredClaimNames.Exp,
                        DateTime.Now + TimeSpan.FromMinutes(120)),
            });
            
            return tokenWriter.WriteToken(payload, logger);
        }
    }
}
