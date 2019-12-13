// @ts-check

const { downloadVSCodeAssets, linkBuiltinStaticExtensions } = require('./utils');

(async () => {
    await downloadVSCodeAssets('stable');
    await downloadVSCodeAssets('insider');
    await linkBuiltinStaticExtensions();
})();
