const { downloadVSCodeAssets } = require('./utils');

(async () => {
    await downloadVSCodeAssets('stable');
    await downloadVSCodeAssets('insider');
})();
