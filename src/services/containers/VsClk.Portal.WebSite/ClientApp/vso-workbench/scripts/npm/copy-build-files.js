const path = require('path');
const copydir = require('copy-dir');

const srcPublicFolder = path.join(__dirname, '../../public');
const distPublicFolder = path.join(__dirname, '../../../public/workbench-page');

copydir.sync(srcPublicFolder, distPublicFolder, {}, function(err){
    if(err) throw err;

    console.log('** Copying workbench-page files done.');
});

const srcExtensionsFolder = path.join(__dirname, '../../node_modules/extensions');
const distExtensionsFolder = path.join(__dirname, '../../../node_modules/extensions');

copydir.sync(srcExtensionsFolder, distExtensionsFolder, {}, function(err){
    if(err) throw err;

    console.log('** Copying workbench-page extensions files done.');
});
