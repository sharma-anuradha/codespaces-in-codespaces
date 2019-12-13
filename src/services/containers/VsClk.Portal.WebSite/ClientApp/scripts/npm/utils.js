// @ts-check

const fs = require('fs');
const path = require('path');
const rimrafCallback = require('rimraf');
const { promisify } = require('util');

const { downloadVSCode } = require('../vscode/download-vscode');
const { getCurrentAssetsCommit } = require('../vscode/get-current-vscode-assets-version');
const {
    vscodeAssetsTargetPathBase,
    assetName,
    packageJsonPath,
    node_modules,
} = require('./constants');

const readFile = promisify(fs.readFile);
const exists = promisify(fs.exists);
const link = promisify(fs.symlink);
const rimraf = promisify(rimrafCallback);

async function getVSCodeCommitFromPackage(quality) {
    try {
        const fileContents = await readFile(packageJsonPath, { encoding: 'utf-8' });
        const packageMetadata = JSON.parse(fileContents);
        return quality === 'stable'
            ? packageMetadata.vscodeCommit.stable
            : packageMetadata.vscodeCommit.insider;
    } catch (ex) {
        console.error('Failed to parse package.json');
    }

    return null;
}

async function downloadVSCodeAssets(quality) {
    downloadVSCodeAssetsInternal(quality, vscodeAssetsTargetPathBase, assetName);
}

async function downloadVSCodeAssetsInternal(quality, vscodeAssetsTargetPathBase, assetName) {
    const requiredCommitId = await getVSCodeCommitFromPackage(quality);
    if (!requiredCommitId) {
        console.log('No vscode commit in package.json. Nothing to do.');
        return;
    }

    const vscodeAssetsTargetPath = path.join(vscodeAssetsTargetPathBase, quality);
    const currentDownloadedAssetsCommit = await getCurrentAssetsCommit(vscodeAssetsTargetPath);
    if (currentDownloadedAssetsCommit && currentDownloadedAssetsCommit !== requiredCommitId) {
        console.log('Current version of assets does not match required version. Removing');

        await rimraf(vscodeAssetsTargetPath);
    }

    if (currentDownloadedAssetsCommit && currentDownloadedAssetsCommit === requiredCommitId) {
        console.log('Asset versions match, nothing to do.');
        return;
    }

    await downloadVSCode(requiredCommitId, assetName, quality, vscodeAssetsTargetPath);
}

async function linkBuiltinStaticExtensions() {
    /** TEMP: VS Code plans to include the extensions that can run in the browser as part of the web-standalone package.
     *  Until we get that we download the server package just to the extensions.
     */
    const vscodeServerAssetsTargetPath = path.join(vscodeAssetsTargetPathBase, 'server');
    downloadVSCodeAssetsInternal('stable', vscodeServerAssetsTargetPath, 'server-linux-x64-web');

    const builtinExtensionsPath = path.join(vscodeServerAssetsTargetPath, 'stable', 'extensions');
    const builtinExtensionsTargetPath = path.join(node_modules, 'extensions');

    const targetExists = await exists(builtinExtensionsTargetPath);
    if (!targetExists) {
        await link(builtinExtensionsPath, builtinExtensionsTargetPath, 'junction');
    }
}

module.exports = {
    getVSCodeCommitFromPackage,
    downloadVSCodeAssets,
    linkBuiltinStaticExtensions,
};
