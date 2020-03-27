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
const { ensureDir } = require('../vscode/fileUtils');

const readFile = promisify(fs.readFile);
const exists = promisify(fs.exists);
const link = promisify(fs.symlink);
const rimraf = promisify(rimrafCallback);

/**
 * @param {string} quality
 */
async function getVSCodeCommitFromPackage(quality) {
    try {
        const fileContents = await readFile(packageJsonPath, { encoding: 'utf-8' });
        const packageMetadata = JSON.parse(fileContents);
        return quality === 'insider'
            ? packageMetadata.vscodeCommit.insider
            : packageMetadata.vscodeCommit.stable;
    } catch (ex) {
        console.error('Failed to parse package.json');
    }

    return null;
}

/**
 * @param {string} quality
 */
async function downloadVSCodeAssets(quality) {
    await downloadVSCodeAssetsInternal(quality, vscodeAssetsTargetPathBase, assetName);
    await linkBuiltinStaticExtensions(quality);
}

/**
 * @param {string} quality
 * @param {string} vscodeAssetsTargetPathBase
 * @param {string} assetName
 */
async function downloadVSCodeAssetsInternal(quality, vscodeAssetsTargetPathBase, assetName) {
    const requiredCommitId = await getVSCodeCommitFromPackage(quality);
    if (!requiredCommitId) {
        console.log('No vscode commit in package.json. Nothing to do.');
        return;
    }

    const vscodeAssetsTargetPath = path.join(
        vscodeAssetsTargetPathBase,
        requiredCommitId.substr(0, 7)
    );
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

    return vscodeAssetsTargetPath;
}

/**
 * @param {string} quality
 */
async function linkBuiltinStaticExtensions(quality) {
    /** TEMP: VS Code plans to include the extensions that can run in the browser as part of the web-standalone package.
     *  Until we get that we download the server package just to the extensions.
     */
    const vscodeServerAssetsTargetPath = path.join(vscodeAssetsTargetPathBase, 'server');
    downloadVSCodeAssetsInternal(quality, vscodeServerAssetsTargetPath, 'server-linux-x64-web');

    const requiredCommitId = await getVSCodeCommitFromPackage(quality);

    const builtinExtensionsPath = path.join(
        vscodeServerAssetsTargetPath,
        requiredCommitId.substr(0, 7),
        'extensions'
    );

    const targetExtensionsFolderPath = path.join(node_modules, 'extensions');
    const builtinExtensionsTargetPath = path.join(targetExtensionsFolderPath, requiredCommitId.substr(0, 7));

    await ensureDir(targetExtensionsFolderPath);
    const targetExists = await exists(builtinExtensionsTargetPath);
    if (!targetExists) {
        await link(builtinExtensionsPath, builtinExtensionsTargetPath, 'junction');
    }
}

module.exports = {
    getVSCodeCommitFromPackage,
    downloadVSCodeAssets,
};
