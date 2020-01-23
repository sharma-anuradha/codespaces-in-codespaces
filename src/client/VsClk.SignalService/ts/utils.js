const cp = require('child_process');
const mkdirp = require('mkdirp');

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

exports.executeCommand = executeCommand;
exports.ensurePath = ensurePath;