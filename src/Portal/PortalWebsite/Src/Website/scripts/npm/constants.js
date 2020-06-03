const path = require('path');

// can also be the server (server-linux-x64-web) if you need a server
const assetName = 'web-standalone';

// project root
const root = path.resolve(__dirname, '..', '..');
const dotnetPortalRoot = path.resolve(
    root,
    '..',
    '..',
    '..',
    '..',
    'services',
    'containers',
    'VsClk.Portal.WebSite'
);

const node_modules = path.join(root, 'node_modules');
const vscodeAssetsTargetPathBase = path.join(root, 'vscode-downloads', 'workbench-page', assetName);
const packageJsonPath = path.join(root, 'packages', 'vso-workbench', 'package.json');
const amdConfigPath = path.join(root, 'public', 'amdconfig.js');

const appSecretsPath = path.resolve(dotnetPortalRoot, 'appsettings.secrets.json');
const devCert = path.resolve(dotnetPortalRoot, 'dev-cert.pfx');
const githubDevCert = path.resolve(dotnetPortalRoot, 'dev-github-cert.pfx');
const githubDotDevCert = path.resolve(dotnetPortalRoot, 'local-github-dev.pfx');

module.exports = {
    root,
    node_modules,
    vscodeAssetsTargetPathBase,
    assetName,
    packageJsonPath,
    amdConfigPath,
    appSecretsPath,
    devCert,
    githubDevCert,
    githubDotDevCert,
};
