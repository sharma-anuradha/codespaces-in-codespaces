
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Audit;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class ClientKeyvaultReader
    {
        private static readonly string SecretName = "Config-ClientKeyvaultSigningKey";

        public static async Task GetKeyvaultKeys()
        {

            var logger = ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>();
            var keyvaultReader = ApplicationServicesProvider.GetRequiredService<IKeyVaultSecretReader>();
            var secrets = await keyvaultReader.GetSecretVersionsAsync(GetAppKeyVaultName(), SecretName, logger);
            var sortedSecrets = secrets.OrderBy(s => DateTime.Now - s.Attributes.Expires.Value);

            var index = 0;
            foreach (var secret in sortedSecrets.Take(2))
            {
                await WriteSecret(secret, index++);
            }

            RuntimeSecrets.ResolveKeychainSettingsSignal();
        }

        private static async Task WriteSecret(KeyVaultSecret secretVersion, int keyIndex)
        {
            var versionString = secretVersion.Identifier.Version;
            
            var logger = ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>();

            var keyvaultReader = ApplicationServicesProvider.GetRequiredService<IKeyVaultSecretReader>();
            var secret = await keyvaultReader.GetSecretAsync(
                GetAppKeyVaultName(),
                SecretName,
                versionString,
                logger
            );

            if (string.IsNullOrEmpty(secret))
            {
                throw new Exception($"Cannot get the secret: {SecretName} version {secretVersion.Identifier.Version}");
            }

            if (keyIndex == 0)
            {
                RuntimeSecrets.KeychainHashKey1 = secret;
                RuntimeSecrets.KeychainHashExpiration1 = secretVersion.Attributes.Expires.Value;
                RuntimeSecrets.KeychainHashId1 = secretVersion.Identifier.Version;

                return;
            }

            RuntimeSecrets.KeychainHashKey2 = secret;
            RuntimeSecrets.KeychainHashExpiration2 = secretVersion.Attributes.Expires.Value;
            RuntimeSecrets.KeychainHashId2 = secretVersion.Identifier.Version;
        }

        public static string GetAppKeyVaultName()
        {
            const string KeyVaultFormat = "vsclk-online-{0}-kv";
            var env = ApplicationServicesProvider.GetRequiredService<IWebHostEnvironment>();
            var appsettings = ApplicationServicesProvider.GetRequiredService<AppSettings>();
            
            if ((env.IsDevelopment()) || appsettings.IsLocal)
            {
                return string.Format(KeyVaultFormat, "dev");
            }
            else if (env.IsStaging())
            {
                return string.Format(KeyVaultFormat, "ppe");
            }
            else if (env.IsProduction())
            {
                return string.Format(KeyVaultFormat, "prod");
            }
            else
            {
                throw new NotSupportedException("Could not determine the key vault name. Unknown hosting environment.");
            }
        }

    }
}
