// @ts-check

const fs = require('fs');
const path = require('path');
const rimrafCallback = require('rimraf');
const { promisify } = require('util');

const { downloadVSCode } = require('../vscode/download-vscode');
const { getCurrentAssetsCommit } = require('../vscode/get-current-vscode-assets-version');

const { vscodeAssetsTargetPathBase, assetName, packageJsonPath } = require('./constants');

const readFile = promisify(fs.readFile);
const rimraf = promisify(rimrafCallback);

async function getVSCodeCommitFromPackage(quality) {
    try {
        const fileContents = await readFile(packageJsonPath, { encoding: 'utf-8' });
        const packageMetadata = JSON.parse(fileContents);
        return quality == 'stable'
            ? packageMetadata.vscodeCommit.stable
            : packageMetadata.vscodeCommit.insider;
    } catch (ex) {
        console.error('Failed to parse package.json');
    }

    return null;
}

async function downloadVSCodeAssets(quality) {
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

module.exports = {
    getVSCodeCommitFromPackage,
    downloadVSCodeAssets,
};
