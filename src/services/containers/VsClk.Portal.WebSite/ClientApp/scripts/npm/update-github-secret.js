const { execSync } = require('child_process');
const { existsSync, readFileSync, writeFileSync } = require('fs');
const { appSecretsPath } = require('./constants');

try {
    function getSecret(secretName) {
        const azureCliResponse = execSync(
            'az keyvault secret show --name "' + secretName + '" --vault-name "vsclk-online-dev-kv" --sub "vsclk-core-dev" -ojson',
            {
                encoding: 'utf-8',
            }
        );
    
        const { value } = JSON.parse(azureCliResponse);
        return value;
    }

    const gitHubAppClientSecret = getSecret('Local-Config-GitHubAppClientSecret');
    const azDevAppClientSecret = getSecret('Local-Config-AzDevAppClientSecret');

    if (existsSync(appSecretsPath)) {
        const settings = readFileSync(appSecretsPath, { encoding: 'utf-8' });
        const afterGitHubSecretUpdate = updateSecret('GitHubAppClientSecret', settings, gitHubAppClientSecret);
        const afterAzDevSecretUpdate = updateSecret('AzDevAppClientSecret', afterGitHubSecretUpdate, azDevAppClientSecret);
        writeFileSync(appSecretsPath, afterAzDevSecretUpdate);

        function updateSecret(secretName, settingsString, newSecret) {
            let oldSecret = new RegExp('^(\\s+)"' + secretName + '":\\s*".+"(,?)$', 'm');
            if (oldSecret.test(settingsString)) {
                return settingsString.replace(
                    oldSecret,
                    `$1"${secretName}": "${newSecret}"$2`
                );
            }

            let appSettingsLine = /^((\s+)"AppSettings":\s*\{)$/m;
            if (appSettingsLine.test(settingsString)) {
                return settingsString.replace(
                    appSettingsLine,
                    `$1\n$2$2"${secretName}": "${newSecret}",`
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
                        GitHubAppClientSecret: gitHubAppClientSecret,
                        AzDevAppClientSecret: azDevAppClientSecret,
                    },
                },
                null,
                2
            )
        );
    }
} catch (err) {
    console.error('Failed to update the "GitHubAppClientSecret" and "AzDevAppClientSecret".', err);
}
