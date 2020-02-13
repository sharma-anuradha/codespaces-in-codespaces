// /*---------------------------------------------------------------------------------------------
//  *  Copyright (c) Microsoft Corporation. All rights reserved.
//  *  Licensed under the MIT License. See License.txt in the project root for license information.
//  *--------------------------------------------------------------------------------------------*/

'use strict';

var execSync = require('child_process').execSync;
var fs = require('fs');
const path = require('path');
const args = require('minimist')(process.argv.slice(2));
const user = args['user'];
const env = args['env'];
const password = args['password'];
const urlPlaceholder = '{{TEST_URL}}';
const userPlaceholder = '{{TEST_USER}}';
const passwordPlaceholder = '{{TEST_PASSWORD}}';
const PROD_URL = 'https://online.visualstudio.com';
const DEV_URL = 'https://online.dev.core.vsengsaas.visualstudio.com';
const PPE_URL = 'https://online-ppe.core.vsengsaas.visualstudio.com';
const isLocal = env && env.toLowerCase() === 'local' ? true : false;
const url = env ? getURL(env) : DEV_URL;
const TEST_DIR = path.join('test', 'uitest', 'actions');
let TEMP_DIR = isLocal
    ? path.join(process.env.TEMP, 'vso-ui-test')
    : path.join(process.env.AGENT_TEMP_DIR, 'vso-ui-test');
const ERROR_DIR = path.join(TEMP_DIR, 'errors');
const ERROR_FILE_NAME = path.join(ERROR_DIR, 'vso-ui-test-errors.log');
const CHROMIUIM_USER_DATA_DIR = path.join(TEMP_DIR, 'chromium-user-data');
console.log(args);

//create user data directory to avoid repeated login during test execution.

if (!fs.existsSync(TEMP_DIR)) {
    fs.mkdirSync(TEMP_DIR);
}

if (!fs.existsSync(CHROMIUIM_USER_DATA_DIR)) {
    fs.mkdirSync(CHROMIUIM_USER_DATA_DIR);
}

// Check if the file has all the mandatory arguments passed.
if (!user) {
    throw new Error(`Missing argument: ${user}`);
}

if (!password) {
    throw new Error(`Missing argument: ${password}`);
}
performTest();

/**
 * We are doing the preparation for each json file with dynamic URL & password.
 * we are making sure to not have hardcoded URLs in the json which comes from recorder
 * we are also making sure to not have the credentials stored in the json file while checking it into repo.
 */
function init(filename) {
    // updating the json files in temp directory
    // by replacing placeholders with user, password & url values passed as arguments.
    return updateTestJsonPlaceHoldersWithArgs(filename);
}

function performTest() {
    const cmd = getCommand();

    preTestExecutions(cmd);
    console.log(`executing test.`);

    // execute the test json in the order that they wanted to be executed.

    postTestExecutions(cmd);
}

function preTestExecutions(cmd) {
    console.log(`executing pre test actions.`);
    execute(cmd, 'vso-login.json');
    //execute(cmd, 'vso-create-plan.json');
}

function postTestExecutions(cmd) {
    console.log(`executing post test actions.`);
    //execute(cmd, 'vso-delete-plan.json');
}

function execute(cmd, filename) {
    try {
        const newFilename = init(path.join(TEST_DIR, filename));
        const execution = execSync(`${cmd} "${newFilename}"`, {
            encoding: 'utf8',
            stdio: 'pipe',
        });
        console.log(execution);
    } catch (e) {
        console.error(e.message);
        console.error(e.stdout);
        logErrorContent(`${e.message}\n ${e.stdout}`);
    }
}

function logErrorContent(error) {
    if (!fs.existsSync(ERROR_DIR)) {
        fs.mkdirSync(ERROR_DIR);
    }

    fs.appendFile(ERROR_FILE_NAME, error + '\r\n', function(err) {
        if (err) {
            throw err;
        }
        console.info(`errors saved to ${ERROR_FILE_NAME} file.`);
    });
}

function getCommand() {
    let cmd = `npx playwright-cli --verbose --user-data-dir=${CHROMIUIM_USER_DATA_DIR}`;
    if (isLocal) {
        cmd = `npx playwright-cli --debug --verbose' --user-data-dir=${CHROMIUIM_USER_DATA_DIR}`;
        console.log('running in debug mode.');
    } else {
        console.log('running in headless mode.');
    }
    return cmd;
}

function getURL(env) {
    switch (env.toLowerCase()) {
        case 'local':
            // to mimic local testing since Authentication works as of now only with DEV domain.
            return DEV_URL;
        case 'dev':
            return DEV_URL;
        case 'ppe':
            return PPE_URL;
        case 'prod':
            return PROD_URL;
        default:
            return DEV_URL;
    }
}

function updateTestJsonPlaceHoldersWithArgs(filename) {
    let newContents = fs.readFileSync(filename, 'utf-8');
    if (!newContents.includes(userPlaceholder)) {
        throw new Error(`${userPlaceholder} placeholder is found missing in the json file.`);
    }

    if (!newContents.includes(passwordPlaceholder)) {
        throw new Error(`${passwordPlaceholder} placeholder is found missing in the json file.`);
    }

    if (!newContents.includes(urlPlaceholder)) {
        throw new Error(`${urlPlaceholder} placeholder is found missing in the json file.`);
    }

    if (
        newContents.includes(DEV_URL) ||
        newContents.includes(PPE_URL) ||
        newContents.includes(PPE_URL)
    ) {
        throw new Error(`Still actual URL is found in the json file.`);
    }

    // creating a directory to have the updated json files/errors in temp
    if (!fs.existsSync(TEMP_DIR)) {
        fs.mkdirSync(TEMP_DIR);
    }

    const newFilename = path.join(TEMP_DIR, path.basename(filename));
    newContents = newContents.replace(new RegExp(userPlaceholder, 'gi'), user);
    newContents = newContents.replace(new RegExp(passwordPlaceholder, 'gi'), password);
    newContents = newContents.replace(new RegExp(urlPlaceholder, 'gi'), url);
    fs.writeFileSync(newFilename, newContents);
    console.debug(`completed processing ${newFilename} file.`);
    return newFilename;
}
