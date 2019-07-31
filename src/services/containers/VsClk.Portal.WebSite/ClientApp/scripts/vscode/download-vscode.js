// @ts-check

const cp = require('child_process');
const https = require('https');
const fs = require('fs');
const path = require('path');
const { promisify } = require('util');

const { ensureDir } = require('./fileUtils');

const mkdir = promisify(fs.mkdir);
const exists = promisify(fs.exists);
const unlink = promisify(fs.unlink);

/**
 * Downloads updated VSCode server based on commitId.
 * Fails if there's no newer version of VSCode
 * @param {string} commitId
 * @param {string} assetName
 * @param {string} quality
 * @param {string} targetFolderPath
 */
async function downloadVSCode(
    commitId,
    assetName,
    quality,
    targetFolderPath,
    getUrl = resolveDownloadUrl
) {
    try {
        const downloadUrl = await getUrl(commitId, assetName, quality);
        const targetExist = await exists(targetFolderPath);
        if (!targetExist) {
            await ensureDir(targetFolderPath);
        }

        const archivePath = path.join(targetFolderPath, `${assetName}.tar.gz`);

        console.log('Downloading from url:', downloadUrl);
        await download(downloadUrl, archivePath);
        console.log(`tar -xf ${archivePath} --strip-components 1`);
        untarSync(archivePath);
        console.log('Removing downloaded archive.');
        await unlink(archivePath);

        console.log('Success');
    } catch (err) {
        console.error(err);
        process.exit(1);
    }
}

/**
 * @param {string} commitId
 * @param {string} assetName
 * @param {string} quality
 * @returns {Promise<string | undefined>} downloadUrl
 */
async function resolveUpdateUrl(commitId, assetName, quality) {
    return (await getUpdateDetails(commitId, assetName, quality)).url;
}

/**
 * @param {string} commitId
 * @param {string} assetName
 * @param {string} quality
 */
async function getUpdateDetails(commitId, assetName, quality) {
    const updateUrl = createUpdaterUrl(commitId, assetName, quality);

    return new Promise((resolve, reject) => {
        https.get(updateUrl, (res) => {
            if (res.statusCode === 204) {
                reject(new Error('There is no update available'));
                return; // no update available
            }

            if (res.statusCode !== 200) {
                reject(new Error('Failed to get JSON'));
                return;
            }

            let data = '';

            res.on('data', (chunk) => (data += chunk));
            res.on('end', () => resolve(JSON.parse(data)));
            res.on('error', (err) => reject(err));
        });
    });
}

/**
 * @param {string} commitId
 * @param {string} assetName
 * @param {string} quality
 */
function resolveDownloadUrl(commitId, assetName, quality) {
    const url = createDownloadUrl(commitId, assetName, quality);

    return new Promise((resolve, reject) => {
        https.get(url, (res) => {
            if (res.statusCode === 302) {
                return resolve(res.headers.location);
            }
            return reject(new Error('Failed to get download url.'));
        });
    });
}

/**
 * @param {string} commitId
 * @param {string} assetName
 * @param {string} quality
 */
function createDownloadUrl(commitId, assetName, quality) {
    return `https://update.code.visualstudio.com/commit:${commitId}/${assetName}/${quality}`;
}

/**
 * @param {string} commitId
 * @param {string} assetName
 * @param {string} quality
 */
function createUpdaterUrl(commitId, assetName, quality) {
    return `https://update.code.visualstudio.com/api/update/${assetName}/${quality}/${commitId}`;
}

/**
 * @param {string} downloadUrl url to download
 * @param {string} destination path
 */
function download(downloadUrl, destination) {
    return new Promise((resolve, reject) => {
        https.get(downloadUrl, (res) => {
            const outStream = fs.createWriteStream(destination);
            outStream.on('close', () => resolve(destination));
            outStream.on('error', reject);

            res.on('error', reject);
            res.pipe(outStream);
        });
    });
}

/**
 * @param {string} source
 */
function untarSync(source) {
    const destination = path.dirname(source);

    // tar does not create extractDir by default
    if (!fs.existsSync(destination)) {
        fs.mkdirSync(destination);
    }

    cp.spawnSync('tar', ['-xf', source, '-C', destination, '--strip-components', '1']);
}
module.exports = {
    downloadVSCode,
    getUpdateDetails,
};
