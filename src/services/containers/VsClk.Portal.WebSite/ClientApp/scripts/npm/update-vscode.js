// @ts-check

const fs = require('fs');
const { promisify } = require('util');

const { getUpdateDetails } = require('../vscode/download-vscode');
const { getVSCodeCommitFromPackage, downloadVSCodeAssets } = require('./utils');

const { assetName, packageJsonPath } = require('./constants');

const readFile = promisify(fs.readFile);
const writeFile = promisify(fs.writeFile);

async function updateVSCodeAssets() {
    const currentCommit = await getVSCodeCommitFromPackage();

    if (!currentCommit) {
        console.log('There in no commit to be updated. Using latest instead');
    }

    try {
        const currentCommitStable = currentCommit.stable;
        let updateDetails = await getUpdateDetails(
            currentCommitStable || 'latest',
            assetName,
            'stable'
        );

        if (updateDetails !== null) {
            console.log(`Updating to stable commit: ${updateDetails.version}`);
            await setVSCodeCommitInPackageJson(updateDetails.version, 'stable');
            await downloadVSCodeAssets('stable');
        }

        const currentCommitInsider = currentCommit.insider;
        updateDetails = await getUpdateDetails(
            currentCommitInsider || 'latest',
            assetName,
            'insider'
        );

        if (updateDetails !== null) {
            console.log(`Updating to insider commit: ${updateDetails.version}`);
            await setVSCodeCommitInPackageJson(updateDetails.version, 'insider');
            await downloadVSCodeAssets('insider');
        }
    } catch (err) {
        console.log(err.message);
    }
}

/**
 * @param {string} commitId
 */
async function setVSCodeCommitInPackageJson(commitId, quality) {
    try {
        const fileContents = await readFile(packageJsonPath, { encoding: 'utf-8' });
        const packageMetadata = JSON.parse(fileContents);

        if (quality == 'stable') {
            packageMetadata.vscodeCommit.stable = commitId;
        } else {
            packageMetadata.vscodeCommit.insider = commitId;
        }
        await writeFile(packageJsonPath, JSON.stringify(packageMetadata, null, 2));
    } catch (ex) {
        console.error('Failed to parse package.json');
        throw ex;
    }
}

updateVSCodeAssets();
