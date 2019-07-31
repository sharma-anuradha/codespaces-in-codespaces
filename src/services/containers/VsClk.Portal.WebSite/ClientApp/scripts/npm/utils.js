// @ts-check

const fs = require('fs');
const rimrafCallback = require('rimraf');
const { promisify } = require('util');

const { downloadVSCode } = require('../vscode/download-vscode');
const { getCurrentAssetsCommit } = require('../vscode/get-current-vscode-assets-version');

const { vscodeAssetsTargetPath, assetName, quality, packageJsonPath } = require('./constants');

const readFile = promisify(fs.readFile);
const rimraf = promisify(rimrafCallback);

async function getVSCodeCommitFromPackage() {
    try {
        const fileContents = await readFile(packageJsonPath, { encoding: 'utf-8' });
        const packageMetadata = JSON.parse(fileContents);

        if (packageMetadata.vscodeCommit) {
            return packageMetadata.vscodeCommit;
        }
    } catch (ex) {
        console.error('Failed to parse package.json');
    }

    return null;
}

async function downloadVSCodeAssets() {
    const requiredCommitId = await getVSCodeCommitFromPackage();
    if (!requiredCommitId) {
        console.log('No vscode commit in package.json. Nothing to do.');
        return;
    }

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
