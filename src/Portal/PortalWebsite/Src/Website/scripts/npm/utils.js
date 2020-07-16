// @ts-check

const fs = require('fs');
const path = require('path');
const rimrafCallback = require('rimraf');
const { promisify } = require('util');

const { downloadVSCode } = require('../vscode/download-vscode');
const { getCurrentAssetsCommit } = require('../vscode/get-current-vscode-assets-version');
const { vscodeAssetsTargetPathBase, assetName, packageJsonPath } = require('./constants');
const { ensureDir } = require('../vscode/fileUtils');

const readFile = promisify(fs.readFile);
const exists = promisify(fs.exists);
const rename = promisify(fs.rename);
const rimraf = promisify(rimrafCallback);

/**
 * @param {string} quality
 * @param {string} commit
 */
const getQualityCommitName = (quality, commit) => {
    return `${quality}-${commit.substr(0, 7)}`;
};

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

    console.log(`** requiredCommitId`, quality, requiredCommitId);

    if (!requiredCommitId) {
        console.log('No vscode commit in package.json. Nothing to do.');
        return;
    }

    const vscodeAssetsTargetPath = path.join(
        vscodeAssetsTargetPathBase,
        getQualityCommitName(quality, requiredCommitId)
    );
    const currentDownloadedAssetsCommit = await getCurrentAssetsCommit(vscodeAssetsTargetPath);
    if (currentDownloadedAssetsCommit && currentDownloadedAssetsCommit !== requiredCommitId) {
        console.log('Current version of assets does not match required version. Removing');

        await rimraf(vscodeAssetsTargetPath);
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
    const requiredCommitId = await getVSCodeCommitFromPackage(quality);
    const version = getQualityCommitName(quality, requiredCommitId);

    const targetExtensionsFolderPath = path.join(vscodeAssetsTargetPathBase, 'extensions');
    const builtinExtensionsTargetPath = path.join(targetExtensionsFolderPath, version);

    await ensureDir(targetExtensionsFolderPath);
    const targetExists = await exists(builtinExtensionsTargetPath);
    if (!targetExists) {
        await downloadVSCodeAssetsInternal(
            quality,
            vscodeServerAssetsTargetPath,
            'server-linux-x64-web'
        );

        const builtinExtensionsPath = path.join(
            vscodeServerAssetsTargetPath,
            version,
            'extensions'
        );

        await rename(builtinExtensionsPath, builtinExtensionsTargetPath);
    }

    await rimraf(vscodeServerAssetsTargetPath);
}

module.exports = {
    getVSCodeCommitFromPackage,
    downloadVSCodeAssets,
    getQualityCommitName,
};
