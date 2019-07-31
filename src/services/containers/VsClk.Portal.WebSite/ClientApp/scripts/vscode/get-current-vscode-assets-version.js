// @ts-check

const fs = require('fs');
const path = require('path');
const { promisify } = require('util');

const readFile = promisify(fs.readFile);
const exists = promisify(fs.exists);

const productFilename = 'product.json';

/**
 * @param {string} vscodeAssetsPath
 * @returns {Promise<string|null>} commit
 */
async function getCurrentAssetsCommit(vscodeAssetsPath) {
    const productMetadataFilePath = path.join(vscodeAssetsPath, productFilename);
    if (!(await exists(productMetadataFilePath))) {
        console.error('There is no product metadata file at path:', productMetadataFilePath);
        return null;
    }

    const fileContents = await readFile(path.join(vscodeAssetsPath, productFilename), {
        encoding: 'utf-8',
    });

    try {
        const productMetadata = JSON.parse(fileContents);

        if (productMetadata.commit) {
            return productMetadata.commit;
        } else {
            console.warn('There is no commit in product metadata file.');
        }

        return null;
    } catch (ex) {
        console.error('Failed to read product.json file');
        return null;
    }
}

module.exports = {
    getCurrentAssetsCommit,
};
