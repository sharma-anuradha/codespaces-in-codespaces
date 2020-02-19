const cp = require('child_process');
const mkdirp = require('mkdirp');
const fs = require('fs').promises;

function executeCommand(cwd, command) {
    console.log(command);
    return new Promise(function(resolve, reject) {
        const p = cp.exec(command, { cwd: cwd }, (err) => {
            if (err) {
                err.showStack = false;
                reject(err);
            }
            resolve();
        });
        p.stdout.pipe(process.stdout);
        p.stderr.pipe(process.stderr);
    });
}

function ensurePath(path) {
    return new Promise(function (resolve, reject) {
        mkdirp(path, function (err) {
            err ? reject(err) : resolve();
        })
    });
}

async function replaceFileContent(path, contentCallback) {
    let fileContent = (await fs.readFile(path)).toString();
    fileContent = contentCallback(fileContent);
    await fs.writeFile(path, fileContent);
}

exports.executeCommand = executeCommand;
exports.ensurePath = ensurePath;
exports.replaceFileContent = replaceFileContent;