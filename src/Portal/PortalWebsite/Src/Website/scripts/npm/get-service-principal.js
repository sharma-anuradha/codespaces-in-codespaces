const { execSync } = require('child_process');
const { existsSync, readFileSync, writeFileSync } = require('fs');
const { appSecretsPath } = require('./constants');

try {
    const azureCliResponse = execSync(
        'az keyvault secret show --name "app-sp-password" --vault-name "vsclk-online-dev-kv"',
        {
            encoding: 'utf-8',
        }
    );

    const { value } = JSON.parse(azureCliResponse);

    if (existsSync(appSecretsPath)) {
        const settings = readFileSync(appSecretsPath, { encoding: 'utf-8' });
        writeFileSync(appSecretsPath, updateSecret(settings, value));

        function updateSecret(settingsString, newSecret) {
            let oldSecret = /^(\s+)"KeyVaultReaderServicePrincipalClientSecret":\s*".+"(,?)$/m;
            if (oldSecret.test(settingsString)) {
                return settingsString.replace(
                    oldSecret,
                    `$1"KeyVaultReaderServicePrincipalClientSecret": "${newSecret}"$2`
                );
            }

            let appSettingsLine = /^((\s+)"AppSettings":\s*\{)$/m;
            if (appSettingsLine.test(settingsString)) {
                return settingsString.replace(
                    appSettingsLine,
                    `$1\n$2$2"KeyVaultReaderServicePrincipalClientSecret": "${newSecret}",`
                );
            }

            return settingsString;
        }
    } else {
        writeFileSync(
            appSecretsPath,
            JSON.stringify(
                {
                    AppSettings: {
                        KeyVaultReaderServicePrincipalClientSecret: value,
                    },
                },
                null,
                2
            )
        );
    }
} catch (err) {
    console.error('Failed to update the "GitHubAppClientSecret".', err);
}
