// A script to copy all the vscode release files and zip them up into vscode.zip
// This is later extracted when we npm install

const fs = require('fs-extra');
const AdmZip = require('adm-zip');

const vscodeRoot = './vscode-remote';
const tempDirectory = './tmp';
const zipOutput = './vscode.zip';
// Clear current temp directory if it exists
fs.removeSync(tempDirectory);

// Copy files across into a temp directory
fs.copySync(`${vscodeRoot}/out`, `${tempDirectory}`);

// Remove any tsconfig.json files so it doesn't upset the build
fs.unlinkSync(`${tempDirectory}/tsconfig.base.json`, () => {} );
fs.unlinkSync(`${tempDirectory}/tsconfig.json`, () => {} );
fs.unlinkSync(`${tempDirectory}/tsconfig.monaco.json`, () => {} );

// Zip the tmp directory files into vscode.zip
var zip = new AdmZip();
zip.addLocalFolder(tempDirectory);
zip.writeZip(zipOutput);

// Remove the vscode-remote directory and temporary static directory
fs.removeSync(tempDirectory);
fs.removeSync(vscodeRoot);