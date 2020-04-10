const path = require('path');

// can also be the server (server-linux-x64-web) if you need a server
const assetName = 'web-standalone';

// project root
const root = path.resolve(__dirname, '..', '..');
const node_modules = path.join(root, 'node_modules');
const vscodeAssetsTargetPathBase = path.join(root, 'public', assetName);
const packageJsonPath = path.join(root, 'package.json');
const amdConfigPath = path.join(root, 'public', 'amdconfig.js');
const versionJson = path.join(root, 'src', 'version.json');
const appSecretsPath = path.resolve(root, '..', 'appsettings.secrets.json');

module.exports = {
    root,
    node_modules,
    vscodeAssetsTargetPathBase,
    assetName,
    packageJsonPath,
    amdConfigPath,
    versionJson,
    appSecretsPath,
};
