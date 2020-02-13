
module.exports = function(gulp, rootDir, options) {
    if (!options) {
        options = {};
    }
    const utils = require('./utils');
    const nbgv = require('./node_modules/nerdbank-gitversioning');
    const fs = require('fs');
    const path = require('path');
    const util = require('util');
    
    const packageJsonFile = path.join(rootDir, 'package.json');
    const packagesDir = path.join(rootDir, '..', 'pkgs');
    const packageJsonFileCopy = path.join(packagesDir, 'package.json');

    const readFile = util.promisify(fs.readFile);
    const copyFile = util.promisify(fs.copyFile);
    const unlink = util.promisify(fs.unlink);
    const mkdir = util.promisify(fs.mkdir);

    function getPackageFileName(packageJson, buildVersion) {
        return `${packageJson.name.replace('@', '').replace('/', '-')}-${buildVersion}.tgz`;
    }
    
    async function mkdirp(dir) {
        try {
            await mkdir(dir);
        } catch (e) {
            if (e.code !== 'EEXIST') throw e;
        }
    }

    async function getPackageFilePath() {
        const packageJson = JSON.parse(await readFile(packageJsonFile));
        const packageFileName = getPackageFileName(
            packageJson,
            (await nbgv.getVersion(rootDir)).npmPackageVersion,
        );

        return path.join(rootDir, packageFileName);
    }

    gulp.task('setPackageVersion', async function() {
        await nbgv.setPackageVersion(rootDir);
        await mkdirp(packagesDir);
        await copyFile(packageJsonFile, packageJsonFileCopy);
        if (options.versionDependencies) {
            let version = (await nbgv.getVersion(rootDir)).npmPackageVersion;
            utils.replaceFileContent(packageJsonFile, function(content) {
                options.versionDependencies.forEach(s => {
                    content = content.replace(s, '^' + version);
                });
                return content;
            });
        }
    });
    
    gulp.task('resetPackageVersion', async function() {
        await copyFile(packageJsonFileCopy, packageJsonFile);
        await unlink(packageJsonFileCopy);
        await nbgv.resetPackageVersionPlaceholder(rootDir);
    });

    gulp.task('compile', function() {
        return utils.executeCommand(rootDir, 'yarn compile');
    });
    
    gulp.task('pack', async function() {
        await utils.executeCommand(rootDir, `npm pack`);
    });
    
    gulp.task('copyPackage', async function() {
        const packageFilePath = await getPackageFilePath();
        return gulp.src([packageFilePath]).pipe(gulp.dest(packagesDir));      
    });

    gulp.task('publish', async function() {
        const registryUri = 'https://devdiv.pkgs.visualstudio.com/_packaging/VS/npm/registry/';

        const packageFilePath = await getPackageFilePath();
        const publishCommand = `npm publish --registry ${registryUri} "${packageFilePath}"`;
        await utils.executeCommand(rootDir, publishCommand);
    });

    gulp.task(
        'pack-pre-publish',
        gulp.series(['compile', 'setPackageVersion', 'pack', 'resetPackageVersion', 'copyPackage']),
        function() {
            return new Promise(function(resolve, reject) {
                resolve();
            });
        },
    );
    
    gulp.task(
        'pack-publish',
        gulp.series(['pack-pre-publish', 'publish']),
        function() {
            return new Promise(function(resolve, reject) {
                resolve();
            });
        },
    );

    return gulp;
};