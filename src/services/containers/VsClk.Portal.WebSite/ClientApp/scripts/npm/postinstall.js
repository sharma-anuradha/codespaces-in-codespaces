// @ts-check

const {
    downloadVSCodeAssets,
    copyStaticExtensions,
    linkBuiltinStaticExtensions,
} = require('./utils');

(async () => {
    await downloadVSCodeAssets('stable');
    await downloadVSCodeAssets('insider');
    await copyStaticExtensions();
    await linkBuiltinStaticExtensions();
})();
