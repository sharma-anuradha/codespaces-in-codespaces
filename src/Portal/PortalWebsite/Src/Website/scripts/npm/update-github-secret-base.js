const { execSync } = require('child_process');
const { existsSync, readFileSync, writeFileSync } = require('fs');
const { appSecretsPath } = require('./constants');

module.exports = {
    updateGithubSecret: (secretName) => {
        try {
            const azureCliResponse = execSync(
                `az keyvault secret show --name "Local-Config-${secretName}" --vault-name "vsclk-online-dev-kv" --sub "vsclk-core-dev" -ojson`,
                {
                    encoding: 'utf-8',
                }
            );
        
            const { value } = JSON.parse(azureCliResponse);
        
            if (existsSync(appSecretsPath)) {
                const settings = readFileSync(appSecretsPath, { encoding: 'utf-8' });
                writeFileSync(appSecretsPath, updateSecret(settings, value));
        
                function updateSecret(settingsString, newSecret) {
                    const oldSecret = new RegExp(`^(\\s+)"${secretName}":\\s*".+"(,?)$`, 'm');
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
                                [secretName]: value,
                            },
                        },
                        null,
                        2
                    )
                );
            }
        } catch (err) {
            console.error(`Failed to update the "${secretName}".`, err);
        }
    }   
}
