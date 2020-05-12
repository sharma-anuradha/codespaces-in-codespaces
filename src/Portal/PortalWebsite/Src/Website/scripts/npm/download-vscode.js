const path = require('path');
const { promisify } = require('util');
const rimrafCallback = require('rimraf');

const { downloadVSCodeAssets } = require('./utils');
const {
    node_modules,
} = require('./constants');

const rimraf = promisify(rimrafCallback);

(async () => {
    await rimraf(path.join(node_modules, 'extensions'));
    
    await downloadVSCodeAssets('stable');
    await downloadVSCodeAssets('insider');
})();
