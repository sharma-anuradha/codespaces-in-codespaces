const stableFlag = 'stable';
const insiderFlag = 'insider';

const params = new URLSearchParams(location.search);
const paramsFeatureSet = params.get('dogfoodChannel');

const vsoFeatureSet = window.localStorage.getItem('vso-featureset');

let vscodeQuality = stableFlag;
if (paramsFeatureSet === insiderFlag) {
    vscodeQuality = insiderFlag;
}

if (vsoFeatureSet === insiderFlag) {
    vscodeQuality = insiderFlag;
}

if (vsoFeatureSet === stableFlag) {
    vscodeQuality = stableFlag;
}

const commits = {
    insider: 'a0fe37870c42e0704a492cdc6b8550b4cf23f63c',
    stable: 'ff915844119ce9485abfe8aa9076ec76b5300ddd',
};

const commitId = commits[vscodeQuality].substr(0, 7);
const vscodeCommitId = `${vscodeQuality}-${commitId}`;

self.require = {
    baseUrl: `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/out`,
    paths: {
        'vscode-textmate': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/vscode-textmate/release/main`,
        'onigasm-umd': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/onigasm-umd/release/main`,
        'vscode-oniguruma': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/vscode-oniguruma/release/main`,
        'xterm': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/xterm/lib/xterm.js`,
        'xterm-addon-search': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/xterm-addon-search/lib/xterm-addon-search.js`,
        'xterm-addon-web-links': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/xterm-addon-web-links/lib/xterm-addon-web-links.js`,
        'semver-umd': `${window.location.origin}/workbench-page/web-standalone/${vscodeCommitId}/node_modules/semver-umd/lib/semver-umd.js`,
    },
};

var loaderScript = document.createElement('script');
loaderScript.src = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/loader.js`;
document.body.appendChild(loaderScript);

var workbenchNlsScript = document.createElement('script');
workbenchNlsScript.src = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.nls.js`;
document.body.appendChild(workbenchNlsScript);

var workbenchScript = document.createElement('script');
workbenchScript.defer = true;
workbenchScript.src = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.js`;
document.body.appendChild(workbenchScript);

var cssLink = document.createElement('link');
cssLink.setAttribute('data-name', 'vs/workbench/workbench.web.api');
cssLink.rel = 'stylesheet';
cssLink.type = 'text/css';
cssLink.href = `/workbench-page/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.css`;
document.head.appendChild(cssLink);

var serverUmdLink = document.createElement('link');
serverUmdLink.rel = 'prefetch';
serverUmdLink.href = `/workbench-page/web-standalone/${vscodeCommitId}/static/node_modules/semver-umd/lib/semver-umd.js`;
document.head.appendChild(serverUmdLink);
