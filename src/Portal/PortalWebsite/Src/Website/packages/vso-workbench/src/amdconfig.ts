import { getPartnerInfoFeatureSet } from './utils/getPartnerInfoFeatureSet';
import { getFeatureSet, setFeatureSet } from '../../vso-client-core/src';

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

    self.require = {
        baseUrl: `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/out`,
        paths: {
            'vscode-textmate': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/vscode-textmate/release/main`,
            'onigasm-umd': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/onigasm-umd/release/main`,
            'vscode-oniguruma': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/vscode-oniguruma/release/main`,
            'xterm': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/xterm/lib/xterm.js?v=2`,
            'xterm-addon-search': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/xterm-addon-search/lib/xterm-addon-search.js?v=2`,
            'xterm-addon-unicode11': `${window.location.origin}/static/remote/web/node_modules/xterm-addon-unicode11/lib/xterm-addon-unicode11.js?v=2`,
            'xterm-addon-webgl': `${window.location.origin}/static/remote/web/node_modules/xterm-addon-webgl/lib/xterm-addon-webgl.js?v=2`,
            'xterm-addon-web-links': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/xterm-addon-web-links/lib/xterm-addon-web-links.js?v=2`,
            'semver-umd': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/semver-umd/lib/semver-umd.js?v=2`,
            'iconv-lite-umd': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/iconv-lite-umd/lib/iconv-lite-umd.js?v=2`,
            'jschardet': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/jschardet/dist/jschardet.min.js?v=2`,
        },
    } as any;

    var loaderScript = document.createElement('script');
    loaderScript.src = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/loader.js?v=2`;
    document.body.appendChild(loaderScript);

    var workbenchNlsScript = document.createElement('script');
    workbenchNlsScript.src = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.nls.js?v=2`;
    document.body.appendChild(workbenchNlsScript);

    var workbenchScript = document.createElement('script');
    workbenchScript.defer = true;
    workbenchScript.src = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.js?v=2`;
    document.body.appendChild(workbenchScript);

    var cssLink = document.createElement('link');
    cssLink.setAttribute('data-name', 'vs/workbench/workbench.web.api');
    cssLink.rel = 'stylesheet';
    cssLink.type = 'text/css';
    cssLink.href = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.css?v=2`;
    document.head.appendChild(cssLink);

    var serverUmdLink = document.createElement('link');
    // since the `prefetch-src` does not have proper browser support https://caniuse.com/?search=prefetch-src
    // and the only workaround is to change the `default-src` to `'self'` which is decreases security of CSP,
    // we can preload instead of prefetching the asset for now as a workaround
    serverUmdLink.rel = 'prefetch';
    serverUmdLink.href = `/workbench-page/web-standalone/${vscodeCommitId}/node_modules/semver-umd/lib/semver-umd.js?v=2`;
    document.head.appendChild(serverUmdLink);
}
