// @ts-check

const fs = require('fs');
const { promisify } = require('util');

const { getUpdateDetails } = require('../vscode/download-vscode');
const { getVSCodeCommitFromPackage, downloadVSCodeAssets } = require('./utils');

const { assetName, quality, packageJsonPath } = require('./constants');

const readFile = promisify(fs.readFile);
const writeFile = promisify(fs.writeFile);

async function updateVSCodeAssets() {
    const currentCommit = await getVSCodeCommitFromPackage();

    if (!currentCommit) {
        console.log('There in no commit to be updated. Using latest instead');
    }

    try {
        const updateDetails = await getUpdateDetails(currentCommit || 'latest', assetName, quality);
        console.log(`Updating to commit: ${updateDetails.version}`);

        await setVSCodeCommitInPackageJson(updateDetails.version);

        await downloadVSCodeAssets();
    } catch (err) {
        console.log(err.message);
    }
}

/**
 * @param {string} commitId
 */
async function setVSCodeCommitInPackageJson(commitId) {
    try {
        const fileContents = await readFile(packageJsonPath, { encoding: 'utf-8' });
        const packageMetadata = JSON.parse(fileContents);

        packageMetadata.vscodeCommit = commitId;

        await writeFile(packageJsonPath, JSON.stringify(packageMetadata, null, 2));
    } catch (ex) {
        console.error('Failed to parse package.json');
        throw ex;
    }
}

updateVSCodeAssets();
