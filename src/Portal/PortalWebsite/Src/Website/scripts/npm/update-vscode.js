// @ts-check

const fs = require('fs');
const path = require('path');
const { promisify } = require('util');
const rimrafCallback = require('rimraf');

const { getUpdateDetails } = require('../vscode/download-vscode');
const { getVSCodeCommitFromPackage, downloadVSCodeAssets } = require('./utils');

const { assetName, packageJsonPath, amdConfigPath, vscodeServerAssetsTargetPath } = require('./constants');

const readFile = promisify(fs.readFile);
const writeFile = promisify(fs.writeFile);
const rimraf = promisify(rimrafCallback);

async function updateVSCodeAssets() {
    await Promise.all([
        updateVSCodeAssetsForQuality('insider'),
        updateVSCodeAssetsForQuality('stable'),
    ]);
    
    // since both the above operations are happening in parallel, deleting of server folder as part of above tasks can fail the other task
    await rimraf(vscodeServerAssetsTargetPath);
}

/**
 * @param {string} quality
 */
async function updateVSCodeAssetsForQuality(quality) {
    const currentCommit = await getVSCodeCommitFromPackage(quality);

    if (!currentCommit) {
        console.log(`There in no commit to be updated for ${quality}. Using latest instead.`);
    }

    try {
        let updateDetails = await getUpdateDetails(currentCommit || 'latest', assetName, quality);

        const newCommitId = (updateDetails)
            ? updateDetails.version
            : currentCommit || 'latest';

        console.log(`** Update details for "${quality}" `, updateDetails);

        if (updateDetails) {
            await setVSCodeCommitInPackageJson(newCommitId, quality);
            await updateVSCodeCommitInAmdConfig(currentCommit, newCommitId);
        }

        await downloadVSCodeAssets(quality);
    } catch (err) {
        console.log(err.message);
    }
}

/**
 * @param {string} commitId
 * @param {string} quality
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
        await writeFile(packageJsonPath, JSON.stringify(packageMetadata, null, 2) + '\n');
    } catch (ex) {
        console.error('Failed to parse package.json');
        throw ex;
    }
}

/**
 * @param {string} oldCommitId
 * @param {string} newCommitId
 */
async function updateVSCodeCommitInAmdConfig(oldCommitId, newCommitId) {
    const fileContents = await readFile(amdConfigPath, { encoding: 'utf-8' });
    const newContent = fileContents.replace(oldCommitId, newCommitId);

    await writeFile(amdConfigPath, newContent);
}

updateVSCodeAssets();
