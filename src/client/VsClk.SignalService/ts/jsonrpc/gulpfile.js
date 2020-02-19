const gulp = require('../gulpfile-pack-publish')(require('gulp'), __dirname, {
    versionDependencies: ['link:../signalr-client']
});