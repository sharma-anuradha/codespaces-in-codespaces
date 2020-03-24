const vsoFeatureSet = window.localStorage.getItem('vso-featureset') || 'stable';
const vscodeQuality = vsoFeatureSet === 'insider' ? 'insider' : 'stable';

const commits = {
    insider: '64b65cf8e401cb98d4ddfc8324cb6635ff6fc2ed',
    stable: 'fe22a9645b44368865c0ba92e2fb881ff1afce94',
};

const vscodeCommitId = commits[vscodeQuality].substr(0, 7);

self.require = {
    baseUrl: `${window.location.origin}/static/web-standalone/${vscodeCommitId}/out`,
    paths: {
        'vscode-textmate': `${window.location.origin}/static/web-standalone/${vscodeCommitId}/node_modules/vscode-textmate/release/main`,
        'onigasm-umd': `${window.location.origin}/static/web-standalone/${vscodeCommitId}/node_modules/onigasm-umd/release/main`,
        xterm: `${window.location.origin}/static/web-standalone/${vscodeCommitId}/node_modules/xterm/lib/xterm.js`,
        'xterm-addon-search': `${window.location.origin}/static/web-standalone/${vscodeCommitId}/node_modules/xterm-addon-search/lib/xterm-addon-search.js`,
        'xterm-addon-web-links': `${window.location.origin}/static/web-standalone/${vscodeCommitId}/node_modules/xterm-addon-web-links/lib/xterm-addon-web-links.js`,
        'semver-umd': `${window.location.origin}/static/web-standalone/${vscodeCommitId}/node_modules/semver-umd/lib/semver-umd.js`,
    },
};

var loaderScript = document.createElement('script');
loaderScript.src = `/static/web-standalone/${vscodeCommitId}/out/vs/loader.js`;
document.body.appendChild(loaderScript);

var workbenchNlsScript = document.createElement('script');
workbenchNlsScript.src = `/static/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.nls.js`;
document.body.appendChild(workbenchNlsScript);

var workbenchScript = document.createElement('script');
workbenchScript.defer = true;
workbenchScript.src = `/static/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.js`;
document.body.appendChild(workbenchScript);

var cssLink = document.createElement('link');
cssLink.setAttribute('data-name', 'vs/workbench/workbench.web.api');
cssLink.rel = 'stylesheet';
cssLink.type = 'text/css';
cssLink.href = `/static/web-standalone/${vscodeCommitId}/out/vs/workbench/workbench.web.api.css`;
document.head.appendChild(cssLink);

var serverUmdLink = document.createElement('link');
serverUmdLink.rel = 'prefetch';
serverUmdLink.href = `/static/web-standalone/${vscodeCommitId}/static/node_modules/semver-umd/lib/semver-umd.js`;
document.head.appendChild(serverUmdLink);
