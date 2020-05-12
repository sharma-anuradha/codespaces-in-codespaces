// @ts-check

const fs = require('fs');
const { dirname, resolve } = require('path');
const { promisify } = require('util');

const getStats = promisify(fs.stat);
const mkdir = promisify(fs.mkdir);

/**
 * @param {string} dirPath
 */
async function isDir(dirPath) {
    let stats;

    try {
        stats = await getStats(dirPath);
    } catch (err) {
        // ignore
    }

    return stats && stats.isDirectory() ? true : false;
}

/**
 * @param {string} dirPath
 */
async function mkdirp(dirPath) {
    const normalizedPath = resolve(dirPath);

    try {
        await mkdir(normalizedPath);
    } catch (err) {
        // in case it could not create the folder (the whole path doesn't exist after mkdir)
        if (err.code === 'ENOENT') {
            mkdirp(dirname(normalizedPath));
            return mkdirp(normalizedPath);
        }

        if (isDir(normalizedPath)) {
            // directory exists no more work to be done
            return;
        }

        // dir doesn't exist, fail
        throw err;
    }
}

/**
 * @param {string} dirPath
 */
async function ensureDir(dirPath) {
    const normalizedPath = resolve(dirPath);

    if (await isDir(normalizedPath)) {
        return;
    }

    await mkdirp(normalizedPath);
}

module.exports.ensureDir = ensureDir;
