
const AdmZip = require('adm-zip');

const vscodeWebRepoName = "@environments/vscode-web"
const vscodeZip = `./node_modules/${vscodeWebRepoName}/vscode.zip`;
const vscodeOutputDir = './public/static/vscode';

const zip = new AdmZip(vscodeZip);
zip.extractAllTo(/*target path*/vscodeOutputDir, /*overwrite*/true);