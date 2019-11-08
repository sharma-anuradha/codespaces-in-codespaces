const path = require('path');

// possible qualities - insider, stable, exploration
const quality = 'stable';
// can also be the server (server-linux-x64-web) if you need a server
const assetName = 'web-standalone';

// project root
const root = path.resolve(__dirname, '..', '..');
const vscodeAssetsTargetPath = path.join(root, 'public', 'static', assetName);
const packageJsonPath = path.join(root, 'package.json');
const appSecretsPath = path.resolve(root, '..', 'appsettings.secrets.json');

module.exports = {
    root,
    vscodeAssetsTargetPath,
    assetName,
    quality,
    packageJsonPath,
    appSecretsPath,
};
