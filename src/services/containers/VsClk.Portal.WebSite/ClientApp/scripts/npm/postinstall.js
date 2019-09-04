// @ts-check

const fs = require('fs');
const { downloadVSCodeAssets } = require('./utils');
const consts = require('./constants');

(async () => {
    await downloadVSCodeAssets();

    // copy product.json and package.json to the root since webpack cannot reach into the the folder that is not under `src/`
    const productFileName = 'product.json';
    const productOutputFileName = 'product-vscode-web.json';
    const packageFileName = 'package.json';
    const packageOutputFileName = 'package-vscode-web.json';
    const srcRoot = './src';

    fs.copyFileSync(
        `${consts.vscodeAssetsTargetPath}/${productFileName}`,
        `${srcRoot}/${productOutputFileName}`
    );

    fs.copyFileSync(
        `${consts.vscodeAssetsTargetPath}/${packageFileName}`,
        `${srcRoot}/${packageOutputFileName}`
    );
})();
