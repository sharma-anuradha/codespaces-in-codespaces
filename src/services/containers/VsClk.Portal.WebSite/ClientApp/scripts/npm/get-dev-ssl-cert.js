const path = require('path');
const atob = require('atob');

const { execSync } = require('child_process');
const { writeFile } = require('fs-extra');

(async () => {
    try {
        const azureCliResponse = execSync(
            'az keyvault secret show --name "dev-core-vsengsaas-visualstudio-com-ssl" --vault-name "vsclk-core-dev-kv" -ojson',
            {
                encoding: 'utf-8',
            }
        );
    
        const { value } = JSON.parse(azureCliResponse);
        const decodedValue = atob(value);
        const certPath = path.join(__dirname, '../../../dev-cert.pfx');

        await writeFile(certPath, decodedValue, 'binary');
    } catch (err) {
        console.error('Failed to update the "dev-cert".', err);
    }
})();
