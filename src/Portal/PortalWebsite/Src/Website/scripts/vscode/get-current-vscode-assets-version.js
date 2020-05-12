// @ts-check

const fs = require('fs');
const path = require('path');
const { promisify } = require('util');

const readFile = promisify(fs.readFile);
const exists = promisify(fs.exists);

const versionFilename = 'version.generated.json';

/**
 * @param {string} vscodeAssetsPath
 * @returns {Promise<string|null>} commit
 */
async function getCurrentAssetsCommit(vscodeAssetsPath) {
    const versionMetadataFilePath = path.join(vscodeAssetsPath, versionFilename);
    if (!(await exists(versionMetadataFilePath))) {
        console.error('There is no version metadata file at path:', versionMetadataFilePath);
        return null;
    }

    const fileContents = await readFile(path.join(vscodeAssetsPath, versionFilename), {
        encoding: 'utf-8',
    });

    try {
        const versionMetadata = JSON.parse(fileContents);

        if (versionMetadata.commit) {
            return versionMetadata.commit;
        } else {
            console.warn('There is no commit in version metadata file.');
        }

        return null;
    } catch (ex) {
        console.error(`Failed to read ${versionFilename} file`);
        return null;
    }
}

module.exports = {
    getCurrentAssetsCommit,
    versionFilename,
};
