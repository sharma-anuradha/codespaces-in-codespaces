// @ts-check
const fs = require('fs');
const { promisify } = require('util');
const { versionJson } = require('./constants');

const writeFile = promisify(fs.writeFile);

async function createVersionJsonFile() {
    try {
        const version = {
            version: process.env.NODE_ENV === 'development' ? 'dev' : Date.now().toString(),
        };

        await writeFile(versionJson, JSON.stringify(version, null, 2));
    } catch (ex) {
        console.error('Failed to parse package.json');
        throw ex;
    }
}

createVersionJsonFile().catch((error) => {
    console.error(error.stack);

    process.exit(1);
});
