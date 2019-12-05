const path = require('path');

// can also be the server (server-linux-x64-web) if you need a server
const assetName = 'web-standalone';

// project root
const root = path.resolve(__dirname, '..', '..');
const vscodeAssetsTargetPathBase = path.join(root, 'public', 'static', assetName);
const packageJsonPath = path.join(root, 'package.json');
const versionJson = path.join(root, 'src', 'version.json');
const appSecretsPath = path.resolve(root, '..', 'appsettings.secrets.json');

module.exports = {
    root,
    vscodeAssetsTargetPathBase,
    assetName,
    packageJsonPath,
    versionJson,
    appSecretsPath,
};
