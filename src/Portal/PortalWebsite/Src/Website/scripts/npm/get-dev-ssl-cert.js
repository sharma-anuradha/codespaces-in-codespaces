const atob = require('atob');

const { execSync } = require('child_process');
const { writeFile } = require('fs-extra');

const {
    devCert,
    githubDevCert,
    githubPFDevCert,
    githubPFCodespacesDevCert,
    githubDotDevCert,
} = require('./constants');

const getCert = async (certName, certOutputPath) => {
    try {
        const azureCliResponse = execSync(
            `az keyvault secret show --name "${certName}" --vault-name "vsclk-core-dev-kv" -ojson`,
            {
                encoding: 'utf-8',
            }
        );

        const { value } = JSON.parse(azureCliResponse);
        const decodedValue = atob(value);

        await writeFile(certOutputPath, decodedValue, 'binary');
    } catch (err) {
        console.error('Failed to update the "dev-cert".', err);
    }
};

getCert('dev-core-vsengsaas-visualstudio-com-ssl', devCert);
getCert('dev-core-workspaces-githubusercontent-com-ssl', githubPFDevCert);
getCert('dev-codespaces-githubusercontent-com-ssl', githubPFCodespacesDevCert);
getCert('workspaces-dev-github-com-ssl', githubDevCert);
getCert('dev-github-dev-ssl', githubDotDevCert);
