using System;
using System.IO;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Services.TokenService.Authentication;
using Microsoft.VsSaaS.Tokens;
using Xunit;
using Xunit.Extensions.AssemblyFixture;

[assembly: TestFramework(AssemblyFixtureFramework.TypeName, AssemblyFixtureFramework.AssemblyName)]

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    public class TokenServiceFixture : IDisposable
    {
        public const string TestAppId1 = "TestAppId1";
        public const string TestAppId2 = "TestAppId2";

        public const string TestIssuer1 = nameof(TestIssuer1);
        public const string TestIssuer2 = nameof(TestIssuer2);

        public const string TestAudience1 = nameof(TestAudience1);
        public const string TestAudience2 = nameof(TestAudience2);
        public const string TestAudience3 = nameof(TestAudience3);

        public static readonly SigningCredentials TestSigningCredentials1 =
            new X509SigningCredentials(GetTestCert("TestIssuer", isPrivate: true));
        public static readonly SigningCredentials TestValidatingCredentials1 =
            new X509SigningCredentials(GetTestCert("TestIssuer", isPrivate: false));
        public static readonly SigningCredentials TestSigningCredentials2 =
            new SigningCredentials(CreateSymmetricKey(), SecurityAlgorithms.HmacSha256);
        public static readonly EncryptingCredentials TestEncryptingCredentials1 =
            new X509EncryptingCredentials(GetTestCert("TestAudience", isPrivate: false));
        public static readonly EncryptingCredentials TestDecryptingCredentials1 =
            new X509EncryptingCredentials(GetTestCert("TestAudience", isPrivate: true));

        public static readonly TimeSpan ExchangeLifetime = TimeSpan.FromHours(1);

        private readonly IWebHost webHost;

        public TokenServiceFixture()
        {
            ServiceUri = new Uri($"http://localhost:9000/");

            Environment.SetEnvironmentVariable(
                "OVERRIDE_APPSETTINGS_JSON", "appsettings.test.json");

            this.webHost = WebHost.CreateDefaultBuilder(Array.Empty<string>())
                .UseEnvironment("Development")
                .UseStartup<TestStartup>()
                .UseUrls(ServiceUri.AbsoluteUri)
                .Build();
            _ = this.webHost.RunAsync();
        }

        public Uri ServiceUri { get; }

        public void Dispose()
        {
            this.webHost.Dispose();
        }

        public static SymmetricSecurityKey CreateSymmetricKey()
        {
            var keyId = Guid.NewGuid().ToString();
            var key = new SymmetricSecurityKey(JwtTokenUtilities.GenerateKeyBytes(256));
            key.KeyId = keyId;
            return key;
        }

        public static X509Certificate2 GetTestCert(string name, bool isPrivate)
        {
            byte[] certBytes;
            var assembly = typeof(TokenServiceFixture).Assembly;
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

        private class TestStartup : Startup
        {
            public TestStartup(IWebHostEnvironment hostingEnvironment) : base(hostingEnvironment)
            {
            }

            protected override string GetSettingsRelativePath()
            {
                var testBinDir = Path.GetDirectoryName(
                    typeof(TokenServiceFixture).Assembly.Location);
                var settingsRelativePath = IsRunningInAzure() ?
                    string.Empty : Path.GetFullPath(testBinDir + Path.DirectorySeparatorChar);
                return settingsRelativePath;
            }

            public override void ConfigureServices(IServiceCollection services)
            {
                base.ConfigureServices(services);

                // Because this TestStartup class is in a different assembly, the main assembly
                // needs to be added as an Application Part so the controllers are discovered.
                services.AddMvc().AddApplicationPart(typeof(Startup).Assembly);
            }

            protected override void ConfigureAuthentication(
                IServiceCollection services)
            {
                services.AddAuthentication()
                    .AddScheme<JwtBearerOptions, MockAuthenticationHandler>(
                        JwtBearerUtility.AadAuthenticationScheme, configureOptions: null);
            }

            protected override void ConfigureTokenHandling(IServiceCollection services)
            {
                var tokenReader = new JwtReader();
                services.AddSingleton<IJwtReader>(tokenReader);
                var tokenWriter = new JwtWriter();
                services.AddSingleton<IJwtWriter>(tokenWriter);

                tokenWriter.AddIssuer(TestIssuer1, TestSigningCredentials1);
                tokenWriter.AddIssuer(TestIssuer2, TestSigningCredentials2);

                tokenWriter.AddAudience(TestAudience1);
                tokenWriter.AddAudience(TestAudience2, TestEncryptingCredentials1);

                tokenReader.AddIssuer(TestIssuer1, new[] { TestValidatingCredentials1 });
                tokenReader.AddIssuer(TestIssuer2, new[] { TestSigningCredentials2 });

                tokenReader.AddAudience(TestAudience1);
                tokenReader.AddAudience(TestAudience2, new[] { TestDecryptingCredentials1 });
            }
        }
    }
}
