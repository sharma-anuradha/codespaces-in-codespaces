const stableFlag = 'stable';
const insiderFlag = 'insider';

const params = new URLSearchParams(location.search);
const paramsFeatureSet = params.get('vscodeChannel');

const vsoFeatureSet = window.localStorage.getItem('vso-featureset');

let vscodeQuality = stableFlag;
if (paramsFeatureSet === insiderFlag) {
    vscodeQuality = insiderFlag;
} else if (paramsFeatureSet === stableFlag) {
    vscodeQuality = stableFlag;
} else if (vsoFeatureSet === insiderFlag) {
    vscodeQuality = insiderFlag;
}

const commits = {
    insider: '635cfbcd0f36bf3562b2f45a4995293869c15db2',
    stable: '58bb7b2331731bf72587010e943852e13e6fd3cf',
};

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
        'xterm-addon-web-links': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/xterm-addon-web-links/lib/xterm-addon-web-links.js?v=2`,
        'semver-umd': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/semver-umd/lib/semver-umd.js?v=2`,
        'iconv-lite-umd': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/iconv-lite-umd/lib/iconv-lite-umd.js?v=2`,
        'jschardet': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/jschardet/dist/jschardet.min.js?v=2`,
    },
};

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
serverUmdLink.rel = 'prefetch';
serverUmdLink.href = `/workbench-page/web-standalone/${vscodeCommitId}/node_modules/semver-umd/lib/semver-umd.js?v=2`;
document.head.appendChild(serverUmdLink);
