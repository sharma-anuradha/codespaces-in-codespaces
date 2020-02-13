// /*---------------------------------------------------------------------------------------------
//  *  Copyright (c) Microsoft Corporation. All rights reserved.
//  *  Licensed under the MIT License. See License.txt in the project root for license information.
//  *--------------------------------------------------------------------------------------------*/

'use strict';

const fs = require('fs');
const path = require('path');
const args = require('minimist')(process.argv.slice(2));
const env = args['env'];
const user = args['user'];
const password = args['password'];
const file = args['file'];
const outputDir = args['output-dir'];
const urlPlaceholder = '{{TEST_URL}}';
const userPlaceholder = '{{TEST_USER}}';
const passwordPlaceholder = '{{TEST_PASSWORD}}';
const PROD_URL = 'https://online.visualstudio.com';
const DEV_URL = 'https://online.dev.core.vsengsaas.visualstudio.com';
const PPE_URL = 'https://online-ppe.core.vsengsaas.visualstudio.com';
const url = env ? getURL(env) : DEV_URL;
const OUTPUT_DIR = path.join(outputDir, 'test-prep');
const ERROR_DIR = path.join(OUTPUT_DIR, 'errors');
const ERROR_FILE_NAME = path.join(ERROR_DIR, 'vso-test-prep-errors.log');
console.log(args);

// Check if the file has all the mandatory arguments passed.
if (!user) {
    throw new Error(`Missing argument: ${user}`);
}

if (!password) {
    throw new Error(`Missing argument: ${password}`);
}

if (!outputDir) {
    throw new Error(`Missing argument: ${outputDir}`);
}

// creating a directory to have the updated json files/errors in temp
if (!fs.existsSync(OUTPUT_DIR)) {
    throw new Error(
        `${outputDir} doesn't exist. Please make sure to have the folder in place before trying again.`
    );
}

updateValuesWithPlaceHolders(file);

/**
 * We are doing the preparation for each json file with dynamic URL & password.
 * we are making sure to not have hardcoded URLs in the json which comes from recorder
 * we are also making sure to not have the credentials stored in the json file while checking it into repo.
 */
function updateValuesWithPlaceHolders(file) {
    if (path.extname(file) === '.json') {
        try {
            const contents = fs.readFileSync(file, 'utf-8');
            const filename = path.basename(file);

            console.log(`executing ${filename} for testing ${url}.`);
            const newFilename = path.join(OUTPUT_DIR, filename);

            /*
             updating the json file with user, password & url placeholders for checking it in repo
             as we don't want to have hard coded user, password, url in the repo for CI/CD builds to execute.
            */
            updateTestJsonWithPlaceHolders(contents, newFilename);
            console.debug(`completed processing ${newFilename} file.`);
        } catch (e) {
            console.error(e.message);
            console.error(e.stdout);
            logErrorContent(`${e.message}\n ${e.stdout}`);
        }
    } else {
        console.error(`${file} is not a JSON file to be executed.`);
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

function updateTestJsonWithPlaceHolders(newContents, filename) {
    if (!newContents) {
        return;
    }

    const isDevURLPresent = newContents.includes(DEV_URL);
    const isPPEURLPresent = newContents.includes(PPE_URL);
    const isPRODURLPresent = newContents.includes(PROD_URL);
    let isUpdated = false;

    if (newContents.includes(user)) {
        console.info(
            `User info is found hence placing a placeholder. Since we don't want to have credentials in the repository.`
        );
        newContents = newContents.replace(new RegExp(user, 'gi'), userPlaceholder);
        isUpdated = true;
    }

    if (newContents.includes(password)) {
        console.info(
            `Password is found hence placing a placeholder. Since we don't want to have credentials in the repository.`
        );
        newContents = newContents.replace(new RegExp(password, 'gi'), passwordPlaceholder);
        isUpdated = true;
    }

    if (isDevURLPresent || isPPEURLPresent || isPRODURLPresent) {
        console.info(
            `Actual URL is found hence placing a placeholder. Since we don't want to have them dynamically updated in CI/CD.`
        );

        newContents = newContents.replace(new RegExp(DEV_URL, 'gi'), urlPlaceholder);
        newContents = newContents.replace(new RegExp(PPE_URL, 'gi'), urlPlaceholder);
        newContents = newContents.replace(new RegExp(PROD_URL, 'gi'), urlPlaceholder);
        isUpdated = true;
    }

    if (!isUpdated) {
        console.info('json file is not updated. Since no values found to be replaced.');
    } else {
        fs.writeFileSync(filename, newContents);
    }
}
