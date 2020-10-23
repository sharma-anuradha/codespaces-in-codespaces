import { getPartnerInfoFeatureSet } from './utils/getPartnerInfoFeatureSet';
import { getFeatureSet, setFeatureSet, FeatureSet } from '../../vso-client-core/src';

export const initAMDConfig = async () => {
    const packageJSON = require('../package.json');

    // Check if partner info contains `vscodeChannel`
    const partnerInfoFeatureSet = await getPartnerInfoFeatureSet();
    if (partnerInfoFeatureSet) {
        setFeatureSet(partnerInfoFeatureSet);
    }

    const vscodeQuality = getFeatureSet();

    const commits = packageJSON.vscodeCommit;
    const commitId = commits[vscodeQuality].substr(0, 7);
    const vscodeCommitId = `${vscodeQuality}-${commitId}`;
    var vscodePath = `/workbench-page/web-standalone/${vscodeCommitId}`;
    var vscodeFullPath = new URL(vscodePath, `${window.location.origin}`).href;
    // Disabling the CDN until https://github.com/microsoft/vscode-internalbacklog/issues/1538 is fixed
    // if (vscodeQuality === FeatureSet.Insider) {
    //     vscodePath = new URL(`/insider/${commits[vscodeQuality]}`, 'https://vscodeweb.azureedge.net').href;
    //     vscodeFullPath = vscodePath;
    // }

    self.require = {
        baseUrl: `${vscodeFullPath}/out`,
        paths: {
            'vscode-textmate': `${vscodeFullPath}/node_modules/vscode-textmate/release/main`,
            'vscode-oniguruma': `${vscodeFullPath}/node_modules/vscode-oniguruma/release/main`,
            'xterm': `${vscodeFullPath}/node_modules/xterm/lib/xterm.js`,
            'xterm-addon-search': `${vscodeFullPath}/node_modules/xterm-addon-search/lib/xterm-addon-search.js`,
            'xterm-addon-unicode11': `${vscodeFullPath}/node_modules/xterm-addon-unicode11/lib/xterm-addon-unicode11.js`,
            'xterm-addon-webgl': `${vscodeFullPath}/node_modules/xterm-addon-webgl/lib/xterm-addon-webgl.js`,
            'semver-umd': `${vscodeFullPath}/node_modules/semver-umd/lib/semver-umd.js`,
            'tas-client-umd': `${vscodeFullPath}/node_modules/tas-client-umd/lib/tas-client-umd.js`,
            'iconv-lite-umd': `${vscodeFullPath}/node_modules/iconv-lite-umd/lib/iconv-lite-umd.js`,
            'jschardet': `${vscodeFullPath}/node_modules/jschardet/dist/jschardet.min.js`,
        },
    } as any;

    var loaderScript = document.createElement('script');
    loaderScript.src = `${vscodePath}/out/vs/loader.js`;
    document.body.appendChild(loaderScript);
    
    var workbenchNlsScript = document.createElement('script');
    workbenchNlsScript.src = `${vscodePath}/out/vs/workbench/workbench.web.api.nls.js`;
    document.body.appendChild(workbenchNlsScript);
    
    var workbenchScript = document.createElement('script');
    workbenchScript.defer = true;
    workbenchScript.src = `${vscodePath}/out/vs/workbench/workbench.web.api.js`;
    document.body.appendChild(workbenchScript);
    
    var cssLink = document.createElement('link');
    cssLink.setAttribute('data-name', 'vs/workbench/workbench.web.api');
    cssLink.rel = 'stylesheet';
    cssLink.type = 'text/css';
    cssLink.href = `${vscodePath}/out/vs/workbench/workbench.web.api.css`;
    document.head.appendChild(cssLink);
    
    var serverUmdLink = document.createElement('link');
    // since the `prefetch-src` does not have proper browser support https://caniuse.com/?search=prefetch-src
    // and the only workaround is to change the `default-src` to `'self'` which is decreases security of CSP,
    // we can preload instead of prefetching the asset for now as a workaround
    serverUmdLink.rel = 'prefetch';
    serverUmdLink.href = `${vscodePath}/node_modules/semver-umd/lib/semver-umd.js`;
    document.head.appendChild(serverUmdLink);
}
