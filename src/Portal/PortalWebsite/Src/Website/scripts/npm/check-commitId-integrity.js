const fs = require('fs');
const path = require('path');

const { promisify } = require('util');
const readFile = promisify(fs.readFile);

const VSCODE_CHANNELS =  ['stable', 'insider'];

const PROJECT_ROOT = path.join(__dirname, '../../');
const OLD_VSCODE_COMMITS_FILE = path.join(PROJECT_ROOT, './public/amdconfig.js');
const NEW_VSCODE_COMMITS_FILE = path.join(PROJECT_ROOT, './packages/vso-workbench/package.json');

const getAmdconfigVSCodeCommitId = async (channel) => {
    const fileContents = await readFile(OLD_VSCODE_COMMITS_FILE, { encoding: 'utf8' });
    const regex = /const\s+?commits\s+?=\s+?{([^;]+)}/gim;
    const commitsMatch = regex.exec(fileContents);

    if (!commitsMatch) {
        throw new Error('Cannot read "amdconfig.js" file.');
    }

    try {
        const commits = eval(`({${commitsMatch[1]}})`);
        const commitId = commits[channel];

        if (!commitId) {
            throw new Error(`Cannot find commitId for "${channel}" in "amdconfig.js" file.`);
        }

        return commitId;
    } catch (e) {
        throw new Error(`Cannot parse vscode commits in "amdconfig.js" file. ${e.message}`);
    }
};

const newWorkbenchPackageJSON = require(NEW_VSCODE_COMMITS_FILE);

const run = async () => {
    for (let channel of VSCODE_CHANNELS) {
        const newWorkbenchCommitId = newWorkbenchPackageJSON.vscodeCommit[channel];
        const oldWorkbenchCommitId = await getAmdconfigVSCodeCommitId(channel);

        if (!oldWorkbenchCommitId) {
            throw new Error(`The VSCode[${channel}] commitId for the old workbench is not found, aborting.`);
        }

        if (!newWorkbenchCommitId) {
            throw new Error(`The VSCode[${channel}] commitId for the new workbench is not found, aborting.`);
        }

        if (newWorkbenchCommitId !== oldWorkbenchCommitId) {
            throw new Error(`The VSCode[${channel}] commitId for old[${oldWorkbenchCommitId}] and new[${newWorkbenchCommitId}] do not match, aborting.`);
        }
    }
};

run();
